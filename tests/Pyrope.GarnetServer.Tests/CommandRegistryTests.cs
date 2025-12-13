using System;
using System.Threading;
using Garnet;
using Garnet.server;
using StackExchange.Redis;
using Xunit;

namespace Pyrope.GarnetServer.Tests
{
    public class CommandRegistryTests : IDisposable
    {
        private readonly Garnet.GarnetServer _server;
        private readonly int _port;

        public CommandRegistryTests()
        {
            _port = 3278 + new Random().Next(1000);
            try {
                // Determine port availability
                 _server = new Garnet.GarnetServer(new string[] { "--port", _port.ToString(), "--bind", "127.0.0.1" });
                 
                 _server.Register.NewCommand("VEC.ADD", Garnet.server.CommandType.ReadModifyWrite, new Pyrope.GarnetServer.Extensions.VectorCommandSet(), new Garnet.server.RespCommandsInfo { Command = (Garnet.server.RespCommand)Pyrope.GarnetServer.Extensions.VectorCommandSet.VEC_ADD, Name = "VEC.ADD" });
                 _server.Register.NewCommand("VEC.SEARCH", Garnet.server.CommandType.Read, new Pyrope.GarnetServer.Extensions.VectorCommandSet(), new Garnet.server.RespCommandsInfo { Command = (Garnet.server.RespCommand)Pyrope.GarnetServer.Extensions.VectorCommandSet.VEC_SEARCH, Name = "VEC.SEARCH" });
                 
                _server.Start();
            } catch {
                 _port = 3278 + new Random().Next(1000);
                 _server = new Garnet.GarnetServer(new string[] { "--port", _port.ToString(), "--bind", "127.0.0.1" });
                 
                 _server.Register.NewCommand("VEC.ADD", Garnet.server.CommandType.ReadModifyWrite, new Pyrope.GarnetServer.Extensions.VectorCommandSet(), new Garnet.server.RespCommandsInfo { Command = (Garnet.server.RespCommand)Pyrope.GarnetServer.Extensions.VectorCommandSet.VEC_ADD, Name = "VEC.ADD" });
                 _server.Register.NewCommand("VEC.SEARCH", Garnet.server.CommandType.Read, new Pyrope.GarnetServer.Extensions.VectorCommandSet(), new Garnet.server.RespCommandsInfo { Command = (Garnet.server.RespCommand)Pyrope.GarnetServer.Extensions.VectorCommandSet.VEC_SEARCH, Name = "VEC.SEARCH" });

                _server.Start();
            }
        }

        public void Dispose()
        {
            _server.Dispose();
        }

        [Fact]
        public void VecAdd_ReturnsStubResponse()
        {
            using var redis = ConnectionMultiplexer.Connect($"127.0.0.1:{_port}");
            var db = redis.GetDatabase();

            // VEC.ADD tenant index id vector meta
            // For now, we just check if it accepts the command and returns a stub/OK
            // Since the components aren't registered yet, this should fail with "unknown command"
            var result = db.Execute("VEC.ADD", "tenant1", "idx1", "doc1", "vector_blob", "meta_json");
            
            Assert.Equal("OK", result.ToString());
        }

        [Fact]
        public void VecSearch_ReturnsStubResponse()
        {
            using var redis = ConnectionMultiplexer.Connect($"127.0.0.1:{_port}");
            var db = redis.GetDatabase();

            // VEC.SEARCH tenant index topK vector
            var result = db.Execute("VEC.SEARCH", "tenant1", "idx1", "10", "vector_blob");
            
            // For stub, we might return empty array or string, but let's assume it returns something indicative
            // Garnet custom commands usually return custom types, but stub might be simple string for now
            Assert.NotNull(result); 
        }
    }
}
