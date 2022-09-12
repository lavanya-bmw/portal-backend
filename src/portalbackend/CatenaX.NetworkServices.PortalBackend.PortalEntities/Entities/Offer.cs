﻿/********************************************************************************
 * Copyright (c) 2021,2022 BMW Group AG
 * Copyright (c) 2021,2022 Contributors to the CatenaX (ng) GitHub Organisation.
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

using CatenaX.NetworkServices.PortalBackend.PortalEntities.Enums;
using System.ComponentModel.DataAnnotations;

namespace CatenaX.NetworkServices.PortalBackend.PortalEntities.Entities;

public class Offer
{
    private Offer()
    {
        Provider = null!;
        OfferType = null!;
        Agreements = new HashSet<Agreement>();
        OfferDescriptions = new HashSet<OfferDescription>();
        OfferDetailImages = new HashSet<OfferDetailImage>();
        Companies = new HashSet<Company>();
        OfferSubscriptions = new HashSet<OfferSubscription>();
        OfferLicenses = new HashSet<OfferLicense>();
        UseCases = new HashSet<UseCase>();
        CompanyUsers = new HashSet<CompanyUser>();
        Tags = new HashSet<OfferTag>();
        SupportedLanguages = new HashSet<Language>();
        Documents = new HashSet<Document>();
        UserRoles = new HashSet<UserRole>();
        AppInstances = new HashSet<AppInstance>();
        OfferAssignedConsents = new HashSet<OfferAssignedConsent>();
    }

    /// <summary>
    /// construtor used for the Attach case
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Offer(Guid id) : this()
    {
        Id = id;
    }
    
    public Offer(Guid id, string provider, DateTimeOffset dateCreated, OfferTypeId offerTypeId) : this()
    {
        Id = id;
        Provider = provider;
        DateCreated = dateCreated;
        OfferTypeId = offerTypeId;
    }
    
    public Guid Id { get; private set; }

    [MaxLength(255)]
    public string? Name { get; set; }

    public DateTimeOffset DateCreated { get; private set; }

    public DateTimeOffset? DateReleased { get; set; }

    [MaxLength(255)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(255)]
    public string? MarketingUrl { get; set; }

    [MaxLength(255)]
    public string? ContactEmail { get; set; }

    [MaxLength(255)]
    public string? ContactNumber { get; set; }

    [MaxLength(255)]
    public string Provider { get; set; }

    public OfferTypeId OfferTypeId { get; set; }

    public Guid? SalesManagerId { get; set; }

    public Guid? ProviderCompanyId { get; set; }

    public OfferStatusId OfferStatusId { get; set; }

    public DateTimeOffset? DateLastChanged { get; set; }

    // Navigation properties
    
    public virtual OfferType? OfferType { get; private set; }
    
    public virtual OfferStatus? OfferStatus{ get; set; }
    public virtual ICollection<Agreement> Agreements { get; private set; }
    public virtual ICollection<OfferDescription> OfferDescriptions { get; private set; }
    public virtual ICollection<OfferDetailImage> OfferDetailImages { get; private set; }
    public virtual ICollection<AppInstance> AppInstances { get; private set; }
    public virtual ICollection<OfferLicense> OfferLicenses { get; private set; }
    public virtual ICollection<Company> Companies { get; private set; }
    public virtual ICollection<OfferSubscription> OfferSubscriptions { get; private set; }
    public virtual ICollection<CompanyUser> CompanyUsers { get; private set; }
    public virtual ICollection<Document> Documents { get; private set; }
    public virtual Company? ProviderCompany { get; set; }
    public virtual CompanyUser? SalesManager { get; set; }
    public virtual ICollection<Language> SupportedLanguages { get; private set; }
    public virtual ICollection<OfferTag> Tags { get; private set; }
    public virtual ICollection<UseCase> UseCases { get; private set; }
    public virtual ICollection<UserRole> UserRoles { get; private set; }
    public virtual ICollection<Consent> Consents { get; private set; }
    public virtual ICollection<OfferAssignedConsent> OfferAssignedConsents { get; private set; }
}