using System;
using System.Buffers;
using Garnet;
using Garnet.common; // Confirmed location of RespMemoryWriter
using Garnet.server;
using Tsavorite.core;

namespace Pyrope.GarnetServer.Extensions
{
    public class VectorCommandSet : CustomRawStringFunctions
    {
        // Command IDs check
        public const int VEC_ADD = 10;
        public const int VEC_UPSERT = 11;
        public const int VEC_DEL = 12;
        public const int VEC_SEARCH = 13;
        
        public override bool InitialUpdater(ReadOnlySpan<byte> key, ref RawStringInput input, Span<byte> value, ref RespMemoryWriter output, ref RMWInfo rmwInfo)
        {
             output.WriteSimpleString("OK");
             return true;
        }

        public override int GetInitialLength(ref RawStringInput input)
        {
            return 0;
        }

        public override int GetLength(ReadOnlySpan<byte> value, ref RawStringInput input)
        {
            return value.Length;
        }

        public override bool InPlaceUpdater(ReadOnlySpan<byte> key, ref RawStringInput input, Span<byte> value, ref int valueLength, ref RespMemoryWriter output, ref RMWInfo rmwInfo)
        {
             output.WriteSimpleString("OK");
             return true;
        }

        public override bool CopyUpdater(ReadOnlySpan<byte> key, ref RawStringInput input, ReadOnlySpan<byte> oldValue, Span<byte> newValue, ref RespMemoryWriter output, ref RMWInfo rmwInfo)
        {
             output.WriteSimpleString("OK");
             return true;
        }

        public override bool Reader(ReadOnlySpan<byte> key, ref RawStringInput input, ReadOnlySpan<byte> value, ref RespMemoryWriter output, ref ReadInfo readInfo)
        {
             output.WriteEmptyArray();
             return true;
        }
    }
}

