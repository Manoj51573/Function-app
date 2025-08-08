using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace DoT.Eforms.Test.Shared;

public static class TestHelpers
{
    public static Mock<ILogger<T>> VerifyLogging<T>(this Mock<ILogger<T>> logger, string expectedMessage = "", LogLevel expectedLogLevel = LogLevel.Debug, Times? times = null, bool isAnyString = false)
    {
        times ??= Times.Once();

        Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedMessage) == 0 || isAnyString;

        logger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == expectedLogLevel),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => state(v, t)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), (Times)times);

        return logger;
    }
}