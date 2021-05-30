// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Localization;

namespace BetterAPI.HealthChecks
{
    public sealed class ServicesHealthCheck : IHealthCheck
    {
        private readonly IStringLocalizer<ServicesHealthCheck> _localizer;
        private readonly IServiceProvider _serviceProvider;

        public ServicesHealthCheck(IStringLocalizer<ServicesHealthCheck> localizer, IServiceProvider serviceProvider)
        {
            _localizer = localizer;
            _serviceProvider = serviceProvider;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            string? description;
            HealthStatus status;
            Exception? exception = default;
            Dictionary<string, object?>? data = default;

            try
            {
                var report = ServicesReport(_serviceProvider);

                status = report.MissingRegistrations?.Count > 0 ? HealthStatus.Degraded : HealthStatus.Healthy;

                if (report.MissingRegistrations != null)
                    data = status == HealthStatus.Healthy
                        ? null
                        : report.MissingRegistrations?.ToDictionary(k => k,
                            v => (object?) report.Services?.FirstOrDefault(x => x.ServiceType == v));

                description = status switch
                {
                    HealthStatus.Healthy => _localizer.GetString("The DI container is correctly configured for this application."),
                    HealthStatus.Degraded =>  _localizer.GetString("The DI container for this application has a missing registration, hiding a runtime exception."),
                    _ => throw new ArgumentOutOfRangeException(nameof(status))
                };
            }
            catch (Exception e)
            {
                status = HealthStatus.Unhealthy;
                description = _localizer.GetString("The DI container health check faulted.");
                exception = e;
            }

            var result = new HealthCheckResult(status, description, exception, data!);
            return Task.FromResult(result);
        }

        public static ServiceReports ServicesReport(IServiceProvider serviceProvider)
        {
            var services = serviceProvider.GetRequiredService<IServiceCollection>();

            var missing = new HashSet<string>();
            var report = new ServiceReports
            {
                MissingRegistrations = missing,
                Services = services.Select(x =>
                {
                    var serviceTypeName = x.ServiceType.Name;
                    var implementationTypeName = x.ImplementationType?.Name;
                    var implementationInstanceName = x.ImplementationInstance?.GetType().Name;

                    string? implementationFactoryTypeName = null;
                    if (x.ImplementationFactory != null)
                    {
                        try
                        {
                            var result = x.ImplementationFactory.Invoke(serviceProvider);
                            implementationFactoryTypeName = result.GetType().Name;
                        }
                        catch (InvalidOperationException ex)
                        {
                            if (ex.Source == "Microsoft.Extensions.DependencyInjection.Abstractions")
                            {
                                var match = Regex.Match(ex.Message, "No service for type '([\\w.]*)'",
                                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

                                if (!match.Success)
                                    return new ServiceReport
                                    {
                                        Lifetime = x.Lifetime,
                                        ImplementationType = implementationTypeName,
                                        ImplementationInstance = implementationInstanceName,
                                        ImplementationFactory = implementationFactoryTypeName,
                                        ServiceType = serviceTypeName
                                    };

                                var typeName = match.Groups[1];
                                missing.Add(typeName.Value);
                            }
                            else
                            {
                                Trace.TraceError(ex.ToString());
                            }
                        }
                    }

                    return new ServiceReport
                    {
                        Lifetime = x.Lifetime,
                        ImplementationType = implementationTypeName,
                        ImplementationInstance = implementationInstanceName,
                        ImplementationFactory = implementationFactoryTypeName,
                        ServiceType = serviceTypeName
                    };
                }).AsList()
            };

            return report;
        }

        public sealed class ServiceReport
        {
            public ServiceLifetime? Lifetime { get; set; }
            public string? ImplementationType { get; set; }
            public string? ImplementationInstance { get; set; }
            public string? ImplementationFactory { get; set; }
            public string? ServiceType { get; set; }
        }

        public sealed class ServiceReports
        {
            public HashSet<string>? MissingRegistrations { get; set; }
            public IList<ServiceReport>? Services { get; set; }
        }
    }
}