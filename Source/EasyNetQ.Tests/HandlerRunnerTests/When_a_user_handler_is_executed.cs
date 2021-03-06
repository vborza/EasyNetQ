﻿// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using FluentAssertions;
using Xunit;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.HandlerRunnerTests
{
    public class When_a_user_handler_is_executed
    {
        private byte[] deliveredBody;
        private MessageProperties deliveredProperties;
        private MessageReceivedInfo deliveredInfo;

        private readonly MessageProperties messageProperties = new MessageProperties
            {
                CorrelationId = "correlation_id"
            };
        private readonly MessageReceivedInfo messageInfo = new MessageReceivedInfo("consumer_tag", 123, false, "exchange", "routingKey", "queue");
        private readonly byte[] messageBody = new byte[0];

        private readonly IModel channel;

        public When_a_user_handler_is_executed()
        {
            var consumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();
            var eventBus = new EventBus();

            var handlerRunner = new HandlerRunner(consumerErrorStrategy, eventBus);

            Func<byte[], MessageProperties, MessageReceivedInfo, Task> userHandler = (body, properties, info) => 
                Task.Factory.StartNew(() =>
                    {
                        deliveredBody = body;
                        deliveredProperties = properties;
                        deliveredInfo = info;
                    });

            var consumer = Substitute.For<IBasicConsumer>();
            channel = Substitute.For<IModel>();
            consumer.Model.Returns(channel);

            var context = new ConsumerExecutionContext(
                userHandler, messageInfo, messageProperties, messageBody, consumer);

            var autoResetEvent = new AutoResetEvent(false);
            eventBus.Subscribe<AckEvent>(x => autoResetEvent.Set());

            handlerRunner.InvokeUserMessageHandler(context);

            autoResetEvent.WaitOne(1000);
        }

        [Fact]
        public void Should_deliver_body()
        {
            deliveredBody.Should().BeSameAs(messageBody);
        }

        [Fact]
        public void Should_deliver_properties()
        {
            deliveredProperties.Should().BeSameAs(messageProperties);
        }

        [Fact]
        public void Should_deliver_info()
        {
            deliveredInfo.Should().BeSameAs(messageInfo);
        }

        [Fact]
        public void Should_ACK_message()
        {
            channel.Received().BasicAck(123, false);
        }
    }
}

// ReSharper restore InconsistentNaming