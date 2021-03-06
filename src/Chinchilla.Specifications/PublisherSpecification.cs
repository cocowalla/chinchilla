﻿using System;
using Chinchilla.Specifications.Messages;
using Machine.Fakes;
using Machine.Specifications;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace Chinchilla.Specifications
{
    public class PublisherSpecification
    {
        [Subject(typeof(Publisher<>))]
        public class when_disposing : WithSubject<Publisher<TestMessage>>
        {
            Because of = () =>
                Subject.Dispose();

            It should_dispose_model = () =>
                The<IModelReference>().WasToldTo(m => m.Dispose());
        }

        [Subject(typeof(Publisher<>))]
        public class when_disposing_multiple_times : WithSubject<Publisher<TestMessage>>
        {
            Because of = () =>
            {
                Subject.Dispose();
                Subject.Dispose();
            };

            It should_dispose_model = () =>
                The<IModelReference>().WasToldTo(m => m.Dispose()).OnlyOnce();
        }

        [Subject(typeof(Publisher<>))]
        public class when_publishing : WithSubject<Publisher<TestMessage>>
        {
            Establish context = () =>
            {
                model = An<IModel>();
                properties = An<IBasicProperties>();

                The<IRouter>().WhenToldTo(r => r.Route(Param.IsAny<TestMessage>())).Return("#");

                The<IModelReference>().WhenToldTo(r => r.Execute(Param.IsAny<Action<IModel>>()))
                    .Callback<Action<IModel>>(act => act(model));

                The<IModelReference>().WhenToldTo(r => r.Execute(Param.IsAny<Func<IModel, IBasicProperties>>()))
                    .Return(properties);
            };

            Because of = () =>
                Subject.Publish(new TestMessage());

            It should_serialize_message = () =>
                The<IMessageSerializer>().WasToldTo(s => s.Serialize(Param.IsAny<IMessage<TestMessage>>()));

            It should_route_message = () =>
                The<IRouter>().WasToldTo(r => r.Route(Param.IsAny<TestMessage>()));

            static IModel model;

            static IBasicProperties properties;
        }

        [Subject(typeof(ConfirmingPublisher<>))]
        public class when_publishing_with_receipt : WithSubject<Publisher<TestMessage>>
        {
            Establish context = () =>
            {
                model = An<IModel>();
                model.WhenToldTo(m => m.NextPublishSeqNo).Return(300);
            };

            Because of = () =>
                receipt = Subject.PublishWithReceipt(
                    new TestMessage(),
                    model,
                    "key",
                    An<IBasicProperties>(),
                    new byte[0]);

            It should_mark_receipt_as_unknown = () =>
                receipt.Status.ShouldEqual(PublishStatus.None);

            It should_send_basic_publish = () =>
                model.WasToldTo(m => m.BasicPublish(
                    Param.IsAny<string>(),
                    Param.IsAny<string>(),
                    Param.IsAny<IBasicProperties>(),
                    Param.IsAny<byte[]>()));

            static IModel model;

            static IPublishReceipt receipt;
        }

        [Subject(typeof(Publisher<>))]
        public class when_creating_properties : with_basic_properties<Publisher<TestMessage>>
        {
            Establish context = () =>
            {
                The<IRouter>().WhenToldTo(r => r.ReplyTo()).Return("reply-to");
                The<IMessageSerializer>().WhenToldTo(s => s.ContentType).Return("content-type");
            };

            Because of = () =>
                properties = Subject.CreateProperties(new TestMessage());

            It should_not_have_correlationId = () =>
                properties.IsCorrelationIdPresent().ShouldBeFalse();

            It should_set_reply_to = () =>
                properties.ReplyTo.ShouldEqual("reply-to");

            It should_set_delivery_mode_to_persistent = () =>
                properties.DeliveryMode.ShouldEqual((byte)2);

            It should_set_content_type_on_properties = () =>
                properties.ContentType.ShouldEqual("content-type");

            static IBasicProperties properties;
        }

        [Subject(typeof(Publisher<>))]
        public class when_creating_properties_with_message_with_correlation_id : with_basic_properties<Publisher<TestRequestMessage>>
        {
            Establish context = () =>
                message = new TestRequestMessage();

            Because of = () =>
                properties = Subject.CreateProperties(message);

            It should_not_have_correlationId = () =>
                properties.IsCorrelationIdPresent().ShouldBeTrue();

            static IBasicProperties properties;

            static TestRequestMessage message;
        }

        [Subject(typeof(Publisher<>))]
        public class when_creating_properties_with_message_with_timeout_support : with_basic_properties<Publisher<TestRequestMessage>>
        {
            Establish context = () =>
                 message = new TestRequestMessage();

            Because of = () =>
                properties = Subject.CreateProperties(message);

            It should_set_expiration = () =>
                properties.IsExpirationPresent().ShouldBeTrue();

            It should_set_expiration_ = () =>
                properties.Expiration.ShouldEqual("60000");

            static IBasicProperties properties;

            static TestRequestMessage message;
        }

        [Subject(typeof(Publisher<>))]
        public class when_creating_properties_with_transient_message : with_basic_properties<Publisher<TransientMessage>>
        {
            Establish context = () =>
                 message = new TransientMessage();

            Because of = () =>
                properties = Subject.CreateProperties(message);

            It should_set_delivery_mode_to_not_persistent = () =>
                properties.DeliveryMode.ShouldEqual((byte)1);

            static IBasicProperties properties;

            static TransientMessage message;
        }

        public class with_basic_properties<TSubject> : WithSubject<TSubject>
            where TSubject : class
        {
            Establish context = () =>
                The<IModelReference>().WhenToldTo(r => r.Execute(Param.IsAny<Func<IModel, IBasicProperties>>()))
                    .Return(new BasicProperties());
        }
    }
}
