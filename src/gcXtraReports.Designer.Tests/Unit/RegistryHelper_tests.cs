﻿using FluentAssertions;
using GeniusCode.XtraReports.Designer.Support;
using Microsoft.Win32;
using NUnit.Framework;

namespace GeniusCode.XtraReports.Designer.Tests.Unit
{
    [TestFixture]
    public class RegistryHelper_tests
    {
        const string junkRegistryValue = "test registry key generated by Unit Test";
        const string subKey = "SOFTWARE\\gcXtraReports.Designer.UnitTests";

        [Test]
        public void Should_create_new_registry_key()
        {

            try
            {
                Registry.CurrentUser.DeleteSubKey(subKey);
            }
            catch
            {
            }


            var helper = new RegistryHelper(subKey);
            var value = helper.AcquireRootPath(junkRegistryValue);

            value.Should().Be(junkRegistryValue);

            var key = Registry.CurrentUser.OpenSubKey(subKey);
            key.Should().NotBeNull();
            key.GetValue("ProjectRootPath").Should().Be(junkRegistryValue);
        }

        [Test]
        public void Should_use_existing_registry_key()
        {
            var helper = new RegistryHelper(subKey);
            var value = helper.AcquireRootPath(junkRegistryValue);

            var value2 = helper.AcquireRootPath("this second value should NOT be persisted to registry");

            value.Should().Be(value2);

        }
    }
}
