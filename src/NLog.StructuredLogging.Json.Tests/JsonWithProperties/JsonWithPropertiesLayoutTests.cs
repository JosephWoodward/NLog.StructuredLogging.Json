using System;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using NLog.Time;
using NUnit.Framework;

namespace NLog.StructuredLogging.Json.Tests.JsonWithProperties
{
    [TestFixture]
    public class JsonWithPropertiesLayoutTests
    {
        private const string LoggerName = "TestLoggerName";
        private const string TestMessage = "This is the test message.";

        private static class TestProperties
        {
            public const string One = "Property One";
            public const int Two = 2;
            public const bool Three = true;
        }

        [Test]
        public void PropertiesAreAppendedToJsonOutput()
        {
            var layout = new JsonWithPropertiesLayout();
            layout.Properties.Add(new StructuredLoggingProperty("One", new SimpleLayout(TestProperties.One)));
            layout.Properties.Add(new StructuredLoggingProperty("Two", new SimpleLayout(TestProperties.Two.ToString())));
            layout.Properties.Add(new StructuredLoggingProperty("Three", new SimpleLayout(TestProperties.Three.ToString())));

            var target = new MemoryTarget
            {
                Name = Guid.NewGuid().ToString(),
                Layout = layout
            };

            SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Trace);

            TimeSource.Current = new FakeTimeSource();
            var logger = LogManager.GetCurrentClassLogger();

            var expectedOutput =
                "{\"TimeStamp\":\"" + TimeSource.Current.Time.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ") + "\"," +
                "\"Level\":\"Trace\"," +
                "\"LoggerName\":\"" + LoggerName +
                "\",\"Message\":\"" + TestMessage + "\"" +
                ",\"One\":\"" + TestProperties.One + "\"" +
                ",\"Two\":\"" + TestProperties.Two + "\"" +
                ",\"Three\":\"" + TestProperties.Three + "\"}";

            var logEvent = new LogEventInfo(LogLevel.Trace, LoggerName, TestMessage);
            logger.Log(logEvent);

            Assert.That(target.Logs.Count, Is.EqualTo(1));

            var output = target.Logs[0];
            output.ShouldBe(expectedOutput);
        }

        [Test]
        public void MachineNameInPropertyIsRendered()
        {
            var layout = new JsonWithPropertiesLayout();
            layout.Properties.Add(new StructuredLoggingProperty("machinename", "${machinename}"));

            var target = new MemoryTarget
            {
                Name = Guid.NewGuid().ToString(),
                Layout = layout
            };

            SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Trace);

            TimeSource.Current = new FakeTimeSource();
            var logger = LogManager.GetCurrentClassLogger();

            var logEvent = new LogEventInfo(LogLevel.Trace, LoggerName, TestMessage);
            logger.Log(logEvent);

            Assert.That(target.Logs.Count, Is.EqualTo(1));

            var output = target.Logs[0];
            Assert.That(output, Does.Contain("\"machinename\":\""));
            Assert.That(output, Does.Not.Contain("${machinename}"));
        }

        [Test]
        public void VarInPropertyIsRendered()
        {
            var layout = new JsonWithPropertiesLayout();
            layout.Properties.Add(new StructuredLoggingProperty("key1", "${var:foo}"));

            var target = new MemoryTarget
            {
                Name = Guid.NewGuid().ToString(),
                Layout = layout
            };

            var fooValue = Guid.NewGuid().ToString();

            SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Trace);
            LogManager.Configuration.Variables.Add("foo", fooValue);

            TimeSource.Current = new FakeTimeSource();
            var logger = LogManager.GetCurrentClassLogger();

            var logEvent = new LogEventInfo(LogLevel.Trace, LoggerName, TestMessage);
            logger.Log(logEvent);

            Assert.That(target.Logs.Count, Is.EqualTo(1));

            var output = target.Logs[0];

            Assert.That(output, Does.Contain(fooValue));
            Assert.That(output, Does.Contain($"\"key1\":\"{fooValue}\""));

            Assert.That(output, Does.Not.Contain("var:"));
            Assert.That(output, Does.Not.Contain("${:"));
            Assert.That(output, Does.Not.Contain("foo"));
        }

        [Test]
        public void VarsInPropertyAreRendered()
        {

            var layout = new JsonWithPropertiesLayout();
            layout.Properties.Add(new StructuredLoggingProperty("key1", "${var:foo}"));
            layout.Properties.Add(new StructuredLoggingProperty("key2", "${var:bar}"));

            var target = new MemoryTarget
            {
                Name = Guid.NewGuid().ToString(),
                Layout = layout
            };

            var fooValue = Guid.NewGuid().ToString();
            var barValue = Guid.NewGuid().ToString();

            SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Trace);
            LogManager.Configuration.Variables.Add("foo", fooValue);
            LogManager.Configuration.Variables.Add("bar", barValue);

            TimeSource.Current = new FakeTimeSource();
            var logger = LogManager.GetCurrentClassLogger();

            var logEvent = new LogEventInfo(LogLevel.Trace, LoggerName, TestMessage);
            logger.Log(logEvent);

            Assert.That(target.Logs.Count, Is.EqualTo(1));

            var output = target.Logs[0];

            Assert.That(output, Does.Contain(fooValue));
            Assert.That(output, Does.Contain(barValue));

            Assert.That(output, Does.Contain($"\"key1\":\"{fooValue}\""));
            Assert.That(output, Does.Contain($"\"key2\":\"{barValue}\""));

            Assert.That(output, Does.Not.Contain("var:"));
            Assert.That(output, Does.Not.Contain("${:"));
            Assert.That(output, Does.Not.Contain("foo"));
            Assert.That(output, Does.Not.Contain("bar"));
        }

        [Test]
        public void VarNotFoundIsNotRendered()
        {

            var layout = new JsonWithPropertiesLayout();
            layout.Properties.Add(new StructuredLoggingProperty("key1", "${var:nosuchvar}"));

            var target = new MemoryTarget
            {
                Name = Guid.NewGuid().ToString(),
                Layout = layout
            };

            var fooValue = Guid.NewGuid().ToString();

            SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Trace);
            LogManager.Configuration.Variables.Add("foo", fooValue);

            TimeSource.Current = new FakeTimeSource();
            var logger = LogManager.GetCurrentClassLogger();

            var logEvent = new LogEventInfo(LogLevel.Trace, LoggerName, TestMessage);
            logger.Log(logEvent);

            Assert.That(target.Logs.Count, Is.EqualTo(1));

            var output = target.Logs[0];

            Assert.That(output, Does.Not.Contain("key1"));
            Assert.That(output, Does.Not.Contain("var:"));
            Assert.That(output, Does.Not.Contain("${:"));
            Assert.That(output, Does.Not.Contain("nosuchvar"));
            Assert.That(output, Does.Not.Contain(fooValue));
        }

        [Test]
        public void PropertyRenderFailure()
        {
            var layout = new JsonWithPropertiesLayout();
            layout.Properties.Add(new StructuredLoggingProperty("One", new FailingLayout()));
            layout.Properties.Add(new StructuredLoggingProperty("Two", new SimpleLayout(TestProperties.Two.ToString())));

            var target = new MemoryTarget
            {
                Name = Guid.NewGuid().ToString(),
                Layout = layout
            };

            SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Trace);

            TimeSource.Current = new FakeTimeSource();
            var logger = LogManager.GetCurrentClassLogger();

            var expectedOutput =
                "{\"TimeStamp\":\"" + TimeSource.Current.Time.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ") + "\"," +
                "\"Level\":\"Trace\"," +
                "\"LoggerName\":\"" + LoggerName +
                "\",\"Message\":\"" + TestMessage + "\"" +
                ",\"One\":\"Render failed: LoggingException Test render fail\"" +
                ",\"Two\":\"" + TestProperties.Two + "\"}";

            var logEvent = new LogEventInfo(LogLevel.Trace, LoggerName, TestMessage);
            logger.Log(logEvent);

            Assert.That(target.Logs.Count, Is.EqualTo(1));

            var output = target.Logs[0];
            output.ShouldBe(expectedOutput);
        }

        [Test]
        public void WhenPropertyNamesAreDuplicated()
        {
            var layout = new JsonWithPropertiesLayout();
            layout.Properties.Add(new StructuredLoggingProperty("duplicate", new SimpleLayout("value1")));
            layout.Properties.Add(new StructuredLoggingProperty("duplicate", new SimpleLayout("value2")));
            layout.Properties.Add(new StructuredLoggingProperty("duplicate", new SimpleLayout("value3")));

            var target = new MemoryTarget
            {
                Name = Guid.NewGuid().ToString(),
                Layout = layout
            };

            SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Trace);

            TimeSource.Current = new FakeTimeSource();
            var logger = LogManager.GetCurrentClassLogger();

            var expectedOutput =
                "{\"TimeStamp\":\"" + TimeSource.Current.Time.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ") + "\"," +
                "\"Level\":\"Trace\"," +
                "\"LoggerName\":\"" + LoggerName +
                "\",\"Message\":\"" + TestMessage + "\"" +
                ",\"duplicate\":\"value1\"" +
                ",\"properties_duplicate\":\"value2\"}";

            var logEvent = new LogEventInfo(LogLevel.Trace, LoggerName, TestMessage);
            logger.Log(logEvent);

            Assert.That(target.Logs.Count, Is.EqualTo(1));

            var output = target.Logs[0];
            output.ShouldBe(expectedOutput);
        }

        public class GetFormattedMessage
        {
            [Test]
            public void AddsPropertyNamePrefixIfPropertyNameIsTheSameAsALogEventPropertyName()
            {
                const string existingPropertyName = "Level";

                TimeSource.Current = new FakeTimeSource();
                var logEvent = new LogEventInfo(LogLevel.Trace, LoggerName, TestMessage);

                var layout = new JsonWithPropertiesLayout();
                layout.Properties.Add(new StructuredLoggingProperty(existingPropertyName, new SimpleLayout(TestProperties.One)));

                var result = layout.Render(logEvent);

                var expectedPropertyName = JsonWithPropertiesLayout.PropertyNamePrefix + existingPropertyName;

                var expectedOutput =
                    "{\"TimeStamp\":\"" + TimeSource.Current.Time.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ") +
                    "\",\"Level\":\"Trace" +
                    "\",\"LoggerName\":\"" + LoggerName +
                    "\",\"Message\":\"" + TestMessage +
                    "\",\"" + expectedPropertyName + "\":\"" + TestProperties.One + "\"}";

                Assert.That(result, Is.EqualTo(expectedOutput));
            }
        }
    }
}
