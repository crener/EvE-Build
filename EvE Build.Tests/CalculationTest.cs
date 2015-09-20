// <copyright file="CalculationTest.cs">Copyright ©  2014</copyright>
using System;
using EvE_Build;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EvE_Build.Tests
{
    /// <summary>This class contains parameterized unit tests for Calculation</summary>
    [PexClass(typeof(Calculation))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestClass]
    public partial class CalculationTest
    {
    }
}
