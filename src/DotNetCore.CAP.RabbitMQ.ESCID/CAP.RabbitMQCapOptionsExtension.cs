// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using DotNetCore.CAP.RabbitMQ;
using DotNetCore.CAP.Transport;
using ESCID.ESP.Messaging.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal sealed class InjectedContractAssemblies
    {
        public Assembly[] Assemblies { get; set; }
    }

    internal sealed class RabbitMQCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<RabbitMQOptions> _configure;
        private readonly RabbitMQOptions _options;

        public RabbitMQCapOptionsExtension(Action<RabbitMQOptions> configure)
        {
            _options = new RabbitMQOptions();
            configure(_options);
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            var rmqConnection = new RabbitMqConnection
            {
                UserName = _options.UserName,
                Password = _options.Password,
                HostNames = _options.HostName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                VirtualHost = "/",
            };

            var assemblies = new InjectedContractAssemblies
            {
                Assemblies = _options.InjectedContractsAssemblies
            };

            services.AddSingleton(assemblies);
            services.AddSingleton(rmqConnection);
            services.AddSingleton<CapMessageQueueMakerService>();
             
            services.Configure(_configure);

            // ESCID
            services.AddRabbitMQ(rmqConnection)
                .EnablePublisherTransactions();

            // Original CAP
            services.AddSingleton<ITransport, RabbitMQTransport>();
        }
    }
}