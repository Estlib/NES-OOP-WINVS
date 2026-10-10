using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    // =========================================================
    // Built-in properties
    // =========================================================

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


    // =========================================================
    // Built-in methods
    // =========================================================

    public sealed record NesBuiltInParameterDefinition(
        string Name,
        SemanticType Type,
        string StorageName
    );


    public sealed record NesBuiltInMethodDefinition(
        string TypeName,
        string MethodName,
        SemanticType ReturnType,
        string RuntimeLabel,
        IReadOnlyList<NesBuiltInParameterDefinition> Parameters
    );


    public static class NesBuiltIns
    {
        // ---------------------------------------------------------
        // Properties
        // ---------------------------------------------------------

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


        // ---------------------------------------------------------
        // Methods
        // ---------------------------------------------------------

        private static readonly Dictionary<string, NesBuiltInMethodDefinition>
            Methods = new(StringComparer.Ordinal)
            {
                {
                    "Screen.SetTile",

                    new NesBuiltInMethodDefinition(
                        "Screen",
                        "SetTile",
                        SemanticType.Void,
                        "NESRT_Screen_SetTile",

                        new[]
                        {
                            new NesBuiltInParameterDefinition(
                                "x",
                                SemanticType.Byte,
                                "NESRT_Screen_SetTile_x"
                            ),

                            new NesBuiltInParameterDefinition(
                                "y",
                                SemanticType.Byte,
                                "NESRT_Screen_SetTile_y"
                            ),

                            new NesBuiltInParameterDefinition(
                                "tile",
                                SemanticType.Byte,
                                "NESRT_Screen_SetTile_tile"
                            )
                        }
                    )
                }
            };


        public static IEnumerable<NesBuiltInMethodDefinition>
            AllMethods => Methods.Values;


        public static bool TryResolveMember(
            IReadOnlyList<string> parts,
            out NesBuiltInDefinition definition
        )
        {
            if (parts.Count != 2)
            {
                definition = null!;
                return false;
            }

            string key =
                $"{parts[0]}.{parts[1]}";

            return Members.TryGetValue(
                key,
                out definition!
            );
        }


        public static bool TryResolveMethod(
            IReadOnlyList<string> parts,
            out NesBuiltInMethodDefinition definition
        )
        {
            if (parts.Count != 2)
            {
                definition = null!;
                return false;
            }

            string key =
                $"{parts[0]}.{parts[1]}";

            return Methods.TryGetValue(
                key,
                out definition!
            );
        }
    }
}
