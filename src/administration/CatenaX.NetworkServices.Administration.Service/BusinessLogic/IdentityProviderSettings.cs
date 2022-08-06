/********************************************************************************
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

using CatenaX.NetworkServices.Framework.ErrorHandling;
using System.Text;

namespace CatenaX.NetworkServices.Administration.Service.BusinessLogic;

public class IdentityProviderSettings
{
    public IdentityProviderCsvSettings CsvSettings { get; set; } = null!;
}

public class IdentityProviderCsvSettings
{
    public string Charset { get; set; } = null!;
    public Encoding Encoding { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string Separator { get; set; } = null!;
    public string HeaderUserId { get; set; } = null!;
    public string HeaderFirstName { get; set; } = null!;
    public string HeaderLastName { get; set; } = null!;
    public string HeaderEmail { get; set; } = null!;
    public string HeaderProviderAlias { get; set; } = null!;
    public string HeaderProviderUserId { get; set; } = null!;
    public string HeaderProviderUserName { get; set; } = null!;
}

public static class IdentityProviderSettingsExtension
{
    public static IServiceCollection ConfigureIdentityProviderSettings(
        this IServiceCollection services,
        IConfigurationSection section) =>
        services.Configure<IdentityProviderSettings>(x =>
            {
                section.Bind(x);
                if(x.CsvSettings == null)
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(x.CsvSettings)} must not be null");
                }
                var csvSettings = x.CsvSettings;
                if (string.IsNullOrWhiteSpace(csvSettings.FileName))
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(csvSettings.FileName)} must not be null or empty");
                }
                if (string.IsNullOrWhiteSpace(csvSettings.ContentType))
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(csvSettings.ContentType)} must not be null or empty");
                }
                try
                {
                    csvSettings.Encoding = Encoding.GetEncoding(csvSettings.Charset);
                }
                catch(ArgumentException ae)
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(csvSettings.Charset)} is not a valid Charset", ae);
                }
                if (string.IsNullOrWhiteSpace(csvSettings.Separator))
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(csvSettings.Separator)} must not be null or empty");
                }
                if (string.IsNullOrWhiteSpace(csvSettings.HeaderUserId))
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(csvSettings.HeaderUserId)} must not be null or empty");
                }
                if (string.IsNullOrWhiteSpace(csvSettings.HeaderFirstName))
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(csvSettings.HeaderFirstName)} must not be null or empty");
                }
                if (string.IsNullOrWhiteSpace(csvSettings.HeaderLastName))
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(csvSettings.HeaderLastName)} must not be null or empty");
                }
                if (string.IsNullOrWhiteSpace(csvSettings.HeaderEmail))
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(csvSettings.HeaderEmail)} must not be null or empty");
                }
                if (string.IsNullOrWhiteSpace(csvSettings.HeaderProviderAlias))
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(csvSettings.HeaderProviderAlias)} must not be null or empty");
                }
                if (string.IsNullOrWhiteSpace(csvSettings.HeaderProviderUserId))
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(csvSettings.HeaderProviderUserId)} must not be null or empty");
                }
                if (string.IsNullOrWhiteSpace(csvSettings.HeaderProviderUserName))
                {
                    throw new ConfigurationException($"{nameof(IdentityProviderSettings)}: {nameof(csvSettings.HeaderProviderUserName)} must not be null or empty");
                }
            });
}
