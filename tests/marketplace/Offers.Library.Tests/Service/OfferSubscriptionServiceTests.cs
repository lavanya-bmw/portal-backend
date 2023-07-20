/********************************************************************************
 * Copyright (c) 2021, 2023 BMW Group AG
 * Copyright (c) 2021, 2023 Contributors to the Eclipse Foundation
 *
 * See the NOTICE file(s) distributed with this work for additional
 * information regarding copyright ownership.
 *
 * This program and the accompanying materials are made available under the
 * terms of the Apache License, Version 2.0 which is available at
 * https://www.apache.org/licenses/LICENSE-2.0.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 *
 * SPDX-License-Identifier: Apache-2.0
 ********************************************************************************/

using Org.Eclipse.TractusX.Portal.Backend.Framework.ErrorHandling;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Models;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Models.Configuration;
using Org.Eclipse.TractusX.Portal.Backend.Mailing.SendMail;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Models;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Repositories;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Entities;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Enums;
using System.Collections.Immutable;

namespace Org.Eclipse.TractusX.Portal.Backend.Offers.Library.Tests.Service;

public class OfferSubscriptionServiceTests
{
    private const string BasePortalUrl = "http//base-url.com";
    private const string ClientId = "Client1";
    private readonly Guid _salesManagerId = new("ac1cf001-7fbc-1f2f-817f-bce058020001");

    private readonly Guid _notAssignedCompanyId;
    private readonly Guid _noBpnSetCompanyId;
    private readonly IdentityData _identity;
    private readonly Guid _companyUserId;
    private readonly Guid _companyId;
    private readonly Guid _existingActiveSubscriptionCompanyId;
    private readonly Guid _existingInactiveSubscriptionCompanyId;
    private readonly Guid _existingOfferId;
    private readonly Guid _existingOfferIdWithoutProviderEmail;
    private readonly Guid _existingOfferWithFailingAutoSetupId;
    private readonly Guid _existingOfferWithoutDetailsFilled;
    private readonly Guid _validSubscriptionId;
    private readonly Guid _newOfferSubscriptionId;
    private readonly Guid _userRoleId;
    private readonly IEnumerable<Guid> _offerAgreementIds;
    private readonly IEnumerable<OfferAgreementConsentData> _validConsentData;
    private readonly IFixture _fixture;
    private readonly IAgreementRepository _agreementRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IConsentRepository _consentRepository;
    private readonly IConsentAssignedOfferSubscriptionRepository _consentAssignedOfferSubscriptionRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IOfferSubscriptionsRepository _offerSubscriptionsRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IPortalRepositories _portalRepositories;
    private readonly IUserRepository _userRepository;
    private readonly IUserRolesRepository _userRolesRepository;
    private readonly IMailingService _mailingService;
    private readonly IProcessStepRepository _processStepRepository;
    private readonly OfferSubscriptionService _sut;

    public OfferSubscriptionServiceTests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization { ConfigureMembers = true });
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _companyUserId = _fixture.Create<Guid>();
        _companyId = _fixture.Create<Guid>();
        _identity = new IdentityData(_fixture.Create<string>(), _companyUserId, IdentityTypeId.COMPANY_USER, _companyId);
        _existingOfferIdWithoutProviderEmail = _fixture.Create<Guid>();
        _existingActiveSubscriptionCompanyId = _fixture.Create<Guid>();
        _existingInactiveSubscriptionCompanyId = _fixture.Create<Guid>();
        _notAssignedCompanyId = _fixture.Create<Guid>();
        _noBpnSetCompanyId = _fixture.Create<Guid>();
        _existingOfferId = _fixture.Create<Guid>();
        _existingOfferWithFailingAutoSetupId = _fixture.Create<Guid>();
        _existingOfferWithoutDetailsFilled = _fixture.Create<Guid>();
        _validSubscriptionId = _fixture.Create<Guid>();
        _newOfferSubscriptionId = _fixture.Create<Guid>();
        _userRoleId = _fixture.Create<Guid>();
        _offerAgreementIds = _fixture.CreateMany<Guid>().ToImmutableArray();
        _validConsentData = _offerAgreementIds.Select(x => new OfferAgreementConsentData(x, ConsentStatusId.ACTIVE));

        _portalRepositories = A.Fake<IPortalRepositories>();
        _agreementRepository = A.Fake<IAgreementRepository>();
        _companyRepository = A.Fake<ICompanyRepository>();
        _consentRepository = A.Fake<IConsentRepository>();
        _consentAssignedOfferSubscriptionRepository = A.Fake<IConsentAssignedOfferSubscriptionRepository>();
        _offerRepository = A.Fake<IOfferRepository>();
        _offerSubscriptionsRepository = A.Fake<IOfferSubscriptionsRepository>();
        _notificationRepository = A.Fake<INotificationRepository>();
        _processStepRepository = A.Fake<IProcessStepRepository>();
        _userRepository = A.Fake<IUserRepository>();
        _userRolesRepository = A.Fake<IUserRolesRepository>();
        _mailingService = A.Fake<IMailingService>();

        SetupRepositories();

        _sut = new OfferSubscriptionService(_portalRepositories, _mailingService);
    }

    #region Add Offer Subscription

    [Theory]
    [InlineData(OfferTypeId.SERVICE)]
    [InlineData(OfferTypeId.APP)]
    public async Task AddOfferSubscription_WithExistingId_CreatesServiceSubscription(OfferTypeId offerTypeId)
    {
        // Arrange
        A.CallTo(() => _offerSubscriptionsRepository.CheckPendingOrActiveSubscriptionExists(A<Guid>._, A<Guid>._, A<OfferTypeId>._))
            .Returns(false);
        var companyAssignedApps = new List<OfferSubscription>();
        A.CallTo(() => _offerSubscriptionsRepository.CreateOfferSubscription(A<Guid>._, A<Guid>._, A<OfferSubscriptionStatusId>._, A<Guid>._, A<Guid>._))
            .Invokes((Guid offerId, Guid companyId, OfferSubscriptionStatusId offerSubscriptionStatusId, Guid requesterId, Guid creatorId) =>
            {
                var companyAssignedApp = new OfferSubscription(_newOfferSubscriptionId, offerId, companyId, offerSubscriptionStatusId, requesterId, creatorId);
                companyAssignedApps.Add(companyAssignedApp);
            });

        // Act
        await _sut.AddOfferSubscriptionAsync(_existingOfferId, _validConsentData, (_identity.UserId, _identity.CompanyId), offerTypeId, BasePortalUrl).ConfigureAwait(false);

        // Assert
        companyAssignedApps.Should().HaveCount(1);
        if (offerTypeId == OfferTypeId.APP)
        {
            A.CallTo(() => _offerSubscriptionsRepository.CheckPendingOrActiveSubscriptionExists(_existingOfferId, _companyId, offerTypeId)).MustHaveHappenedOnceExactly();
        }
        A.CallTo(() => _processStepRepository.CreateProcessStepRange(A<IEnumerable<(ProcessStepTypeId ProcessStepTypeId, ProcessStepStatusId ProcessStepStatusId, Guid ProcessId)>>.That.Matches(x => x.Count() == 1 && x.Single().ProcessStepTypeId == ProcessStepTypeId.TRIGGER_PROVIDER))).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mailingService.SendMails(A<string>._, A<Dictionary<string, string>>._, A<List<string>>._)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(OfferTypeId.SERVICE)]
    [InlineData(OfferTypeId.APP)]
    public async Task AddOfferSubscription_WithSalesManagerEqualsReceiver_CreatesServiceSubscription(OfferTypeId offerTypeId)
    {
        // Arrange
        A.CallTo(() => _offerSubscriptionsRepository.CheckPendingOrActiveSubscriptionExists(A<Guid>._, A<Guid>._, A<OfferTypeId>._))
            .Returns(false);
        var companyAssignedApps = new List<OfferSubscription>();
        A.CallTo(() => _offerSubscriptionsRepository.CreateOfferSubscription(A<Guid>._, A<Guid>._, A<OfferSubscriptionStatusId>._, A<Guid>._, A<Guid>._))
            .Invokes((Guid offerId, Guid companyId, OfferSubscriptionStatusId offerSubscriptionStatusId, Guid requesterId, Guid creatorId) =>
            {
                var companyAssignedApp = new OfferSubscription(_newOfferSubscriptionId, offerId, companyId, offerSubscriptionStatusId, requesterId, creatorId);
                companyAssignedApps.Add(companyAssignedApp);
            });

        // Act
        await _sut.AddOfferSubscriptionAsync(_existingOfferId, _validConsentData, (_identity.UserId, _identity.CompanyId), offerTypeId, BasePortalUrl).ConfigureAwait(false);

        // Assert
        companyAssignedApps.Should().HaveCount(1);
        A.CallTo(() => _mailingService.SendMails(A<string>._, A<Dictionary<string, string>>._, A<List<string>>._)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(OfferTypeId.SERVICE)]
    [InlineData(OfferTypeId.APP)]
    public async Task AddOfferSubscription_WithExistingIdWithoutProviderEmail_CreatesServiceSubscription(OfferTypeId offerTypeId)
    {
        // Arrange 
        A.CallTo(() => _offerSubscriptionsRepository.CheckPendingOrActiveSubscriptionExists(A<Guid>._, A<Guid>._, A<OfferTypeId>._))
            .Returns(false);
        var companyAssignedApps = new List<OfferSubscription>();
        A.CallTo(() => _offerSubscriptionsRepository.CreateOfferSubscription(A<Guid>._, A<Guid>._, A<OfferSubscriptionStatusId>._, A<Guid>._, A<Guid>._))
            .Invokes((Guid offerId, Guid companyId, OfferSubscriptionStatusId offerSubscriptionStatusId, Guid requesterId, Guid creatorId) =>
            {
                var companyAssignedApp = new OfferSubscription(_newOfferSubscriptionId, offerId, companyId, offerSubscriptionStatusId, requesterId, creatorId);
                companyAssignedApps.Add(companyAssignedApp);
            });

        // Act
        await _sut.AddOfferSubscriptionAsync(_existingOfferIdWithoutProviderEmail, _validConsentData, (_identity.UserId, _identity.CompanyId), offerTypeId, BasePortalUrl).ConfigureAwait(false);

        // Assert
        companyAssignedApps.Should().HaveCount(1);
        A.CallTo(() => _processStepRepository.CreateProcessStepRange(A<IEnumerable<(ProcessStepTypeId ProcessStepTypeId, ProcessStepStatusId ProcessStepStatusId, Guid ProcessId)>>.That.Matches(x => x.Count() == 1 && x.Single().ProcessStepTypeId == ProcessStepTypeId.TRIGGER_PROVIDER))).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mailingService.SendMails(A<string>._, A<Dictionary<string, string>>._, A<List<string>>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task AddOfferSubscription_WithExistingAppSubscriptionAndProcessSteps_SkipsProcessStepsCreation()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        A.CallTo(() => _offerSubscriptionsRepository.CheckPendingOrActiveSubscriptionExists(A<Guid>._, A<Guid>._, A<OfferTypeId>._))
            .Returns(false);

        // Act
        await _sut.AddOfferSubscriptionAsync(_existingOfferId, _validConsentData, (_identity.UserId, _identity.CompanyId), OfferTypeId.APP, BasePortalUrl).ConfigureAwait(false);

        // Assert
        A.CallTo(() => _offerSubscriptionsRepository.CheckPendingOrActiveSubscriptionExists(_existingOfferId, _companyId, OfferTypeId.APP)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _offerSubscriptionsRepository.CreateOfferSubscription(A<Guid>._, A<Guid>._, A<OfferSubscriptionStatusId>._, A<Guid>._, A<Guid>._)).MustHaveHappened();
        A.CallTo(() => _offerSubscriptionsRepository.AttachAndModifyOfferSubscription(subscriptionId, A<Action<OfferSubscription>>._)).MustNotHaveHappened();
        A.CallTo(() => _processStepRepository.CreateProcess(A<ProcessTypeId>._)).MustHaveHappened();
        A.CallTo(() => _processStepRepository.CreateProcessStepRange(A<IEnumerable<(ProcessStepTypeId, ProcessStepStatusId, Guid)>>._)).MustHaveHappened();
    }

    [Fact]
    public async Task AddOfferSubscription_WithExistingActiveAppSubscription_Throws()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        A.CallTo(() => _offerSubscriptionsRepository.CheckPendingOrActiveSubscriptionExists(A<Guid>._, A<Guid>._, A<OfferTypeId>._))
            .Returns(true);

        var Act = () => _sut.AddOfferSubscriptionAsync(_existingOfferId, _validConsentData, (_identity.UserId, _identity.CompanyId), OfferTypeId.APP, BasePortalUrl);

        // Act
        var result = await Assert.ThrowsAsync<ConflictException>(Act).ConfigureAwait(false);

        // Assert
        A.CallTo(() => _offerSubscriptionsRepository.CheckPendingOrActiveSubscriptionExists(_existingOfferId, _companyId, OfferTypeId.APP)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _offerSubscriptionsRepository.CreateOfferSubscription(A<Guid>._, A<Guid>._, A<OfferSubscriptionStatusId>._, A<Guid>._, A<Guid>._)).MustNotHaveHappened();
        A.CallTo(() => _offerSubscriptionsRepository.AttachAndModifyOfferSubscription(subscriptionId, A<Action<OfferSubscription>>._)).MustNotHaveHappened();
        A.CallTo(() => _processStepRepository.CreateProcess(A<ProcessTypeId>._)).MustNotHaveHappened();
        A.CallTo(() => _processStepRepository.CreateProcessStepRange(A<IEnumerable<(ProcessStepTypeId, ProcessStepStatusId, Guid)>>._)).MustNotHaveHappened();
        result.Message.Should().Be($"company {_companyId} is already subscribed to {_existingOfferId}");
    }

    [Theory]
    [InlineData(OfferTypeId.SERVICE)]
    [InlineData(OfferTypeId.APP)]
    public async Task AddOfferSubscription_NotAssignedCompany_ThrowsException(OfferTypeId offerTypeId)
    {
        // Arrange
        var identity = _fixture.Build<IdentityData>()
            .With(x => x.CompanyId, _notAssignedCompanyId)
            .Create();

        // Act
        async Task Action() => await _sut.AddOfferSubscriptionAsync(_existingOfferId, new List<OfferAgreementConsentData>(), (identity.UserId, identity.CompanyId), offerTypeId, BasePortalUrl).ConfigureAwait(false);

        // Assert
        var ex = await Assert.ThrowsAsync<ControllerArgumentException>(Action);
        ex.ParamName.Should().Be("companyId");
        ex.Message.Should().Be($"Company {identity.CompanyId} does not exist (Parameter 'companyId')");
    }

    [Theory]
    [InlineData(OfferTypeId.SERVICE)]
    [InlineData(OfferTypeId.APP)]
    public async Task AddOfferSubscription_WithNotExistingId_ThrowsException(OfferTypeId offerTypeId)
    {
        // Arrange
        var notExistingServiceId = Guid.NewGuid();

        // Act
        async Task Action() => await _sut.AddOfferSubscriptionAsync(notExistingServiceId, new List<OfferAgreementConsentData>(), (_identity.UserId, _identity.CompanyId), offerTypeId, BasePortalUrl).ConfigureAwait(false);

        // Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(Action);
        ex.Message.Should().Be($"Offer {notExistingServiceId} does not exist");
    }

    [Theory]
    [InlineData(OfferTypeId.SERVICE)]
    [InlineData(OfferTypeId.APP)]
    public async Task AddOfferSubscription_WithoutOfferProviderDetails_ThrowsException(OfferTypeId offerTypeId)
    {
        // Act
        async Task Action() => await _sut.AddOfferSubscriptionAsync(_existingOfferWithoutDetailsFilled, new List<OfferAgreementConsentData>(), (_identity.UserId, _identity.CompanyId), offerTypeId, BasePortalUrl).ConfigureAwait(false);

        // Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(Action);
        ex.Message.Should().Be("The offer name has not been configured properly");
    }

    [Theory]
    [InlineData(OfferTypeId.SERVICE)]
    [InlineData(OfferTypeId.APP)]
    public async Task AddOfferSubscription_WithMissingConsentData_ThrowsControllerArgumentException(OfferTypeId offerTypeId)
    {
        // Arrange
        var consentData = Enumerable.Empty<OfferAgreementConsentData>();

        // Act
        async Task Action() => await _sut.AddOfferSubscriptionAsync(_existingOfferId, consentData, (_identity.UserId, _identity.CompanyId), offerTypeId, BasePortalUrl).ConfigureAwait(false);

        // Assert
        var ex = await Assert.ThrowsAsync<ControllerArgumentException>(Action);
        ex.ParamName.Should().Be("offerAgreementConsentData");
        ex.Message.Should().EndWith($"must be given for offer {_existingOfferId} (Parameter 'offerAgreementConsentData')");
        ex.Message.Should().ContainAll(_offerAgreementIds.Select(id => id.ToString()));
    }

    [Theory]
    [InlineData(OfferTypeId.SERVICE)]
    [InlineData(OfferTypeId.APP)]
    public async Task AddOfferSubscription_WithAdditionalConsentData_ThrowsControllerArgumentException(OfferTypeId offerTypeId)
    {
        // Arrange
        var additional = _fixture.CreateMany<OfferAgreementConsentData>().ToImmutableArray();
        var consentData = _validConsentData.Concat(additional).ToImmutableArray();

        // Act
        async Task Action() => await _sut.AddOfferSubscriptionAsync(_existingOfferId, consentData, (_identity.UserId, _identity.CompanyId), offerTypeId, BasePortalUrl).ConfigureAwait(false);

        // Assert
        var ex = await Assert.ThrowsAsync<ControllerArgumentException>(Action);
        ex.ParamName.Should().Be("offerAgreementConsentData");
        ex.Message.Should().EndWith($"are not valid for offer {_existingOfferId} (Parameter 'offerAgreementConsentData')");
        ex.Message.Should().ContainAll(additional.Select(consent => consent.AgreementId.ToString()));
    }

    [Theory]
    [InlineData(OfferTypeId.SERVICE)]
    [InlineData(OfferTypeId.APP)]
    public async Task AddOfferSubscription_WithInactiveConsentData_ThrowsControllerArgumentException(OfferTypeId offerTypeId)
    {
        // Arrange
        var consentData = _offerAgreementIds.Select(id => new OfferAgreementConsentData(id, ConsentStatusId.INACTIVE)).ToImmutableArray();

        // Act
        async Task Action() => await _sut.AddOfferSubscriptionAsync(_existingOfferId, consentData, (_identity.UserId, _identity.CompanyId), offerTypeId, BasePortalUrl).ConfigureAwait(false);

        // Assert
        var ex = await Assert.ThrowsAsync<ControllerArgumentException>(Action);
        ex.ParamName.Should().Be("offerAgreementConsentData");
        ex.Message.Should().EndWith($"must be given for offer {_existingOfferId} (Parameter 'offerAgreementConsentData')");
        ex.Message.Should().ContainAll(_offerAgreementIds.Select(id => id.ToString()));
    }

    [Theory]
    [InlineData(OfferTypeId.SERVICE)]
    [InlineData(OfferTypeId.APP)]
    public async Task AddOfferSubscription_WithoutBuisnessPartnerNumber_ThrowsConflictException(OfferTypeId offerTypeId)
    {
        // Act
        var identity = _fixture.Build<IdentityData>()
            .With(x => x.CompanyId, _noBpnSetCompanyId)
            .Create();
        async Task Action() => await _sut.AddOfferSubscriptionAsync(_existingOfferId, new List<OfferAgreementConsentData>(), (identity.UserId, identity.CompanyId), offerTypeId, BasePortalUrl).ConfigureAwait(false);

        // Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(Action);
        ex.Message.Should().Contain("has no BusinessPartnerNumber assigned");
    }

    #endregion

    #region APP - Specialcases

    [Fact]
    public async Task AddOfferSubscription_WithExistingActiveSubscription_ThrowsConflictException()
    {
        // Arrange 
        var identity = _fixture.Build<IdentityData>()
            .With(x => x.CompanyId, _existingActiveSubscriptionCompanyId)
            .Create();
        A.CallTo(() => _offerSubscriptionsRepository.CheckPendingOrActiveSubscriptionExists(_existingOfferId, identity.CompanyId, A<OfferTypeId>._))
            .Returns(true);

        // Act
        async Task Act() => await _sut.AddOfferSubscriptionAsync(_existingOfferId, _validConsentData, (identity.UserId, identity.CompanyId), OfferTypeId.APP, BasePortalUrl).ConfigureAwait(false);

        // Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(Act);
        ex.Message.Should().Contain(" is already subscribed to ");
    }

    #endregion

    #region GetOfferSubscriptionFilterStatusIds

    [Theory]
    [InlineData(OfferSubscriptionStatusId.ACTIVE, new[] { OfferSubscriptionStatusId.ACTIVE })]
    [InlineData(OfferSubscriptionStatusId.INACTIVE, new[] { OfferSubscriptionStatusId.INACTIVE })]
    [InlineData(OfferSubscriptionStatusId.PENDING, new[] { OfferSubscriptionStatusId.PENDING })]
    [InlineData(null, new[] { OfferSubscriptionStatusId.PENDING, OfferSubscriptionStatusId.ACTIVE })]
    public void GetOfferSubscriptionFilterStatusIds_ReturnsExpected(OfferSubscriptionStatusId? offerStatusIdFilter, IEnumerable<OfferSubscriptionStatusId> expectedStatusIds)
    {
        // Act
        var result = OfferSubscriptionService.GetOfferSubscriptionFilterStatusIds(offerStatusIdFilter);

        // Assert
        result.Should().ContainInOrder(expectedStatusIds);
    }

    #endregion

    #region Setup

    private void SetupRepositories()
    {
        var offerDetailData = _fixture.CreateMany<ServiceOverviewData>(5);
        var paginationResult = (int skip, int take) => Task.FromResult(new Pagination.Source<ServiceOverviewData>(offerDetailData.Count(), offerDetailData.Skip(skip).Take(take)));
        var offerDetail = _fixture.Build<OfferDetailData>()
            .With(x => x.Id, _existingOfferId)
            .Create();

        A.CallTo(() => _companyRepository.GetOwnCompanyInformationAsync(_identity.CompanyId))
            .Returns(new CompanyInformationData(_companyId, "The Company", "DE", "BPM00000001"));
        A.CallTo(() => _companyRepository.GetOwnCompanyInformationAsync(_notAssignedCompanyId))
            .Returns((CompanyInformationData?)null);
        A.CallTo(() => _companyRepository.GetOwnCompanyInformationAsync(_noBpnSetCompanyId))
            .Returns(new CompanyInformationData(_companyId, "The Company", "DE", null));
        A.CallTo(() => _companyRepository.GetOwnCompanyInformationAsync(_existingActiveSubscriptionCompanyId))
            .Returns(new CompanyInformationData(_existingActiveSubscriptionCompanyId, "The Company", "DE", "BPM00000001"));
        A.CallTo(() => _companyRepository.GetOwnCompanyInformationAsync(_existingInactiveSubscriptionCompanyId))
            .Returns(new CompanyInformationData(_existingInactiveSubscriptionCompanyId, "The Company", "DE", "BPM00000001"));
        A.CallTo(() => _userRepository.GetServiceProviderCompanyUserWithRoleIdAsync(A<Guid>.That.Matches(x => x == _existingOfferId), A<List<Guid>>.That.Matches(x => x.Count == 1 && x.All(y => y == _userRoleId))))
            .Returns(new List<Guid> { _companyUserId, _salesManagerId }.ToAsyncEnumerable());
        A.CallTo(() => _userRolesRepository.GetUserRoleIdsUntrackedAsync(A<IEnumerable<UserRoleConfig>>.That.Matches(x => x.First(y => y.ClientId == ClientId).ClientId == "Service Manager")))
            .Returns(new List<Guid> { _userRoleId }.ToAsyncEnumerable());
        A.CallTo(() => _userRolesRepository.GetUserRoleIdsUntrackedAsync(A<IEnumerable<UserRoleConfig>>.That.Not.Matches(x => x.First(y => y.ClientId == ClientId).ClientId == "Service Manager")))
            .Returns(new List<Guid>().ToAsyncEnumerable());

        A.CallTo(() => _offerRepository.GetActiveServicesPaginationSource(A<ServiceOverviewSorting>._, A<ServiceTypeId>._, A<string>._))
            .Returns(paginationResult);
        A.CallTo(() => _offerRepository.GetOfferDetailByIdUntrackedAsync(_existingOfferId, A<string>.That.Matches(x => x == "en"), A<Guid>._, A<OfferTypeId>._))
            .Returns(offerDetail with
            {
                OfferSubscriptionDetailData = new[]
            {
                new OfferSubscriptionStateDetailData(Guid.NewGuid(), OfferSubscriptionStatusId.ACTIVE)
            }
            });
        A.CallTo(() => _offerRepository.GetOfferDetailByIdUntrackedAsync(A<Guid>.That.Not.Matches(x => x == _existingOfferId), A<string>._, A<Guid>._, A<OfferTypeId>._))
            .Returns((OfferDetailData?)null);

        A.CallTo(() => _offerRepository.GetOfferProviderDetailsAsync(A<Guid>.That.Matches(x => x == _existingOfferId), A<OfferTypeId>._))
            .Returns(new OfferProviderDetailsData("Test Offer", "Test Company", "provider@mail.de", _salesManagerId, "https://www.testurl.com", false));
        A.CallTo(() => _offerRepository.GetOfferProviderDetailsAsync(A<Guid>.That.Matches(x => x == _existingOfferIdWithoutProviderEmail), A<OfferTypeId>._))
            .Returns(new OfferProviderDetailsData("Test Offer", "Test Company", null, _salesManagerId, "https://www.testurl.com", false));
        A.CallTo(() => _offerRepository.GetOfferProviderDetailsAsync(A<Guid>.That.Matches(x => x == _existingOfferWithFailingAutoSetupId), A<OfferTypeId>._))
            .Returns(new OfferProviderDetailsData("Test Offer", "Test Company", "provider@mail.de", _salesManagerId, "https://www.fail.com", false));
        A.CallTo(() => _offerRepository.GetOfferProviderDetailsAsync(A<Guid>.That.Matches(x => x == _existingOfferWithoutDetailsFilled), A<OfferTypeId>._))
            .Returns(new OfferProviderDetailsData(null, "Test Company", null, _salesManagerId, "https://www.fail.com", false));
        A.CallTo(() => _offerRepository.GetOfferProviderDetailsAsync(A<Guid>.That.Not.Matches(x => x == _existingOfferId || x == _existingOfferWithFailingAutoSetupId || x == _existingOfferWithoutDetailsFilled || x == _existingOfferIdWithoutProviderEmail), A<OfferTypeId>._))
            .Returns((OfferProviderDetailsData?)null);

        var offerSubscription = _fixture.Create<OfferSubscription>();
        A.CallTo(() => _offerSubscriptionsRepository.GetSubscriptionDetailDataForOwnUserAsync(
                A<Guid>.That.Matches(x => x == _validSubscriptionId),
                A<Guid>.That.Matches(x => x == _companyId),
                A<OfferTypeId>._))
            .Returns(new SubscriptionDetailData(_existingOfferId, "Super Offer", OfferSubscriptionStatusId.ACTIVE));
        A.CallTo(() => _offerSubscriptionsRepository.GetSubscriptionDetailDataForOwnUserAsync(
                A<Guid>.That.Not.Matches(x => x == _validSubscriptionId),
                A<Guid>.That.Matches(x => x == _companyId),
                A<OfferTypeId>._))
            .Returns((SubscriptionDetailData?)null);
        A.CallTo(() => _offerSubscriptionsRepository.GetCompanyIdWithAssignedOfferForCompanyUserAndSubscriptionAsync(
                A<Guid>.That.Matches(x => x == _existingOfferId), A<Guid>.That.Matches(x => x == _identity.UserId), A<OfferTypeId>._))
            .Returns((_companyId, offerSubscription));
        A.CallTo(() => _offerSubscriptionsRepository.GetCompanyIdWithAssignedOfferForCompanyUserAndSubscriptionAsync(
                A<Guid>.That.Not.Matches(x => x == _existingOfferId), A<Guid>.That.Matches(x => x == _identity.UserId),
                A<OfferTypeId>._))
            .Returns((_companyId, null));
        A.CallTo(() => _offerSubscriptionsRepository.GetCompanyIdWithAssignedOfferForCompanyUserAndSubscriptionAsync(
                A<Guid>.That.Matches(x => x == _existingOfferId), A<Guid>.That.Not.Matches(x => x == _identity.UserId),
                A<OfferTypeId>._))
            .Returns(((Guid companyId, OfferSubscription? offerSubscription))default);

        A.CallTo(() => _agreementRepository.GetAgreementIdsForOfferAsync(A<Guid>.That.Matches(id => id == _existingOfferId || id == _existingOfferWithFailingAutoSetupId || id == _existingOfferWithoutDetailsFilled || id == _existingOfferIdWithoutProviderEmail)))
            .Returns(_offerAgreementIds.ToAsyncEnumerable());
        A.CallTo(() => _agreementRepository.GetAgreementIdsForOfferAsync(A<Guid>.That.Not.Matches(id => id == _existingOfferId || id == _existingOfferWithFailingAutoSetupId || id == _existingOfferWithoutDetailsFilled || id == _existingOfferIdWithoutProviderEmail)))
            .Returns(_fixture.CreateMany<Guid>().ToAsyncEnumerable());

        A.CallTo(() => _portalRepositories.GetInstance<IAgreementRepository>()).Returns(_agreementRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IConsentRepository>()).Returns(_consentRepository);
        A.CallTo(() => _portalRepositories.GetInstance<ICompanyRepository>()).Returns(_companyRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IConsentAssignedOfferSubscriptionRepository>()).Returns(_consentAssignedOfferSubscriptionRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IOfferRepository>()).Returns(_offerRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IOfferSubscriptionsRepository>()).Returns(_offerSubscriptionsRepository);
        A.CallTo(() => _portalRepositories.GetInstance<INotificationRepository>()).Returns(_notificationRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IProcessStepRepository>()).Returns(_processStepRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IUserRepository>()).Returns(_userRepository);
        A.CallTo(() => _portalRepositories.GetInstance<IUserRolesRepository>()).Returns(_userRolesRepository);
        _fixture.Inject(_portalRepositories);
    }

    #endregion
}
