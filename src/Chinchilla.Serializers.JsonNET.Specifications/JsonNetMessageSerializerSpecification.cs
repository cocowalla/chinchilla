﻿using System;
using Machine.Fakes;
using Machine.Specifications;
using Newtonsoft.Json;

namespace Chinchilla.Serializers.JsonNET.Specifications
{
    public class JsonNetMessageSerializerSpecification
    {
        [Subject(typeof(JsonNetMessageSerializer))]
        public class in_general : WithSubject<JsonNetMessageSerializer>
        {
            It should_have_content_type = () =>
                Subject.ContentType.ShouldEqual("application/json");
        }

        [Subject(typeof(JsonNetMessageSerializer))]
        public class when_serializing : WithSubject<JsonNetMessageSerializer>
        {
            Because of = () =>
                serialized = Subject.Serialize(Message.Create(new InterestingFact()));

            It should_serialize = () =>
                serialized.Length.ShouldBeGreaterThan(0);

            static byte[] serialized;
        }

        [Subject(typeof(JsonNetMessageSerializer))]
        public class when_deserializing : WithSubject<JsonNetMessageSerializer>
        {
            Establish context = () =>
                serialized = Subject.Serialize(
                    Message.Create(new InterestingFact("Disney's Tangled is the 3rd most expensive film ever made...")));

            Because of = () =>
                deserialized = Subject.Deserialize<InterestingFact>(serialized);

            It should_deserialize = () =>
                deserialized.Body.FactBody.ShouldEqual("Disney's Tangled is the 3rd most expensive film ever made...");

            static byte[] serialized;

            static IMessage<InterestingFact> deserialized;
        }

        [Subject(typeof(JsonNetMessageSerializer))]
        public class when_deserializing_with_settings
        {
            Establish context = () =>
            {
                Subject = new JsonNetMessageSerializer(new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                });

                serialized = Subject.Serialize(
                    Message.Create(new ReallyInterestingFact(
                        "Disney's Tangled is the 3rd most expensive film ever made...",
                        "Tangled was Disney’s first full-length computer-animated fairy-tale adventure")));
            };

            Because of = () =>
                exception = Catch.Exception(() => Subject.Deserialize<InterestingFact>(serialized));

            It should_respect_settings_and_throw = () =>
                exception.ShouldBeOfExactType<JsonSerializationException>();

            static JsonNetMessageSerializer Subject;

            static byte[] serialized;

            static Exception exception;
        }

        public class ReallyInterestingFact : InterestingFact
        {
            public ReallyInterestingFact() { }

            public ReallyInterestingFact(string body, string reallyInterestingBody)
                : base(body)
            {
                FactReallyInterestingBody = reallyInterestingBody;
            }

            public string FactReallyInterestingBody { get; set; }
        }

        public class InterestingFact
        {
            public InterestingFact() { }

            public InterestingFact(string body)
            {
                FactBody = body;
            }

            public string FactBody { get; set; }
        }
    }

}
