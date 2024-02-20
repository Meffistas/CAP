// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using ESCID.ESP.Messaging.RabbitMQ;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class RabbitMQTransport : ITransport
    {
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly ILogger _logger;
        private readonly RabbitMqConnection _rabbitMqConnection;
        private readonly InjectedContractAssemblies _injectedContractAssemblies;

        public RabbitMQTransport(
            ILogger<RabbitMQTransport> logger,
            IRabbitMqPublisher rabbitMqPublisher,
            RabbitMqConnection rabbitMqConnection,
            InjectedContractAssemblies injectedContractAssemblies)
        {
            _logger = logger;
            _rabbitMqPublisher = rabbitMqPublisher;
            _rabbitMqConnection = rabbitMqConnection;
            _injectedContractAssemblies = injectedContractAssemblies;
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("RabbitMQ", string.Join(",", _rabbitMqConnection.HostNames));

        public Task<OperateResult> SendAsync(TransportMessage message)
        {
            try
            {
                string json = Encoding.UTF8.GetString(message.Body);
                string messageFullName = message.GetName();
                Type messageType = _injectedContractAssemblies.Assemblies
                    .SelectMany(x => x.GetTypes())
                    .FirstOrDefault(x => x.FullName == messageFullName);
                if (messageType == null)
                    throw new InvalidOperationException($"Type '{messageFullName}' was not found.");

                MethodInfo methodDeserialize = Array.Find(typeof(JsonConvert).GetMethods(), x => x.Name == "DeserializeObject" && x.IsGenericMethod && x.GetParameters().Length == 1);

                if (methodDeserialize == null)
                    throw new InvalidOperationException("DeserializeObject method not found.");

                MethodInfo methodDeserializeGeneric = methodDeserialize.MakeGenericMethod(messageType);
                var typedMessagePayload = methodDeserializeGeneric.Invoke(null, new object[] { json });

                MethodInfo methodPublish = _rabbitMqPublisher.GetType().GetMethod("Publish");
                if (methodPublish == null)
                    throw new InvalidOperationException("Publish method not found.");

                MethodInfo methodPublishGeneric = methodPublish.MakeGenericMethod(messageType);
                methodPublishGeneric.Invoke(_rabbitMqPublisher, new object[] { typedMessagePayload, null });

                _logger.LogDebug($"RabbitMQ topic message [{message.GetName()}] has been published.");

                return Task.FromResult(OperateResult.Success);
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);
                var errors = new OperateError
                {
                    Code = ex.HResult.ToString(),
                    Description = ex.Message
                };

                return Task.FromResult(OperateResult.Failed(wrapperEx, errors));
            }
        }
    }
}