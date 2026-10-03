using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public enum NesBuiltInMember
    {
        ScreenBackgroundColor
    }


    public sealed record NesBuiltInDefinition(
        string TypeName,
        string MemberName,
        NesBuiltInMember Member,
        SemanticType ValueType,
        bool CanRead,
        bool CanWrite
    );


    public static class NesBuiltIns
    {
        private static readonly Dictionary<string, NesBuiltInDefinition>
            Members = new(StringComparer.Ordinal)
            {
                {
                    "Screen.BackgroundColor",

                    new NesBuiltInDefinition(
                        "Screen",
                        "BackgroundColor",
                        NesBuiltInMember.ScreenBackgroundColor,
                        SemanticType.Byte,
                        CanRead: false,
                        CanWrite: true
                    )
                }
            };


        public static bool TryResolveMember(
            IReadOnlyList<string> parts,
            out NesBuiltInDefinition? definition
        )
        {
            definition = null;


            if (parts.Count != 2)
            {
                return false;
            }


            string key =
                $"{parts[0]}.{parts[1]}";


            return Members.TryGetValue(
                key,
                out definition
            );
        }
    }
}
