using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Mpb.Consensus.Logic.BlockLogic;
using System.Numerics;
using Mpb.Consensus.Model;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Mpb.Consensus.Logic.Exceptions;
using Mpb.Consensus.Contract;

namespace Mpb.Consensus.Test.Logic
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class TransactionValidatorTest
    {
        Mock<ITimestamper> _timestamper;
        SHA256 _sha256;

        [TestInitialize]
        public void Initialize()
        {
            _timestamper = new Mock<ITimestamper>(MockBehavior.Strict);
            _sha256 = SHA256.Create();
        }

        

        [TestCleanup]
        public void Cleanup()
        {
            _timestamper.VerifyAll();
        }
    }
}
