// Guids.cs
// MUST match guids.h
using System;

namespace PeterPalotas.EventSourceClassGenerator
{
    static class GuidList
    {
        public const string guidEventSourceClassGeneratorPkgString = "c3a98197-7250-4e47-911f-f9056a89dac1";
        public const string guidEventSourceClassGeneratorCmdSetString = "939197d3-b81c-45f0-bb89-4ef3392007b8";

        public static readonly Guid guidEventSourceClassGeneratorCmdSet = new Guid(guidEventSourceClassGeneratorCmdSetString);
    };
}