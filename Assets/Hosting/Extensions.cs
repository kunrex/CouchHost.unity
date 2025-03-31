using System;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

using Hosting.Structs;

namespace Hosting
{
    public static class Extensions
    {
        public static readonly byte WebsocketCloseCode = 0x8;
        private static readonly char[] Mapping = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
        
        public static (string, string) GetLocalIPAddress()
        {
            foreach (var ip in NetworkInterface.GetAllNetworkInterfaces()
                         .Where(x => x.OperationalStatus == OperationalStatus.Up)
                         .SelectMany(x => x.GetIPProperties().UnicastAddresses)
                         .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork))
            {
                var stringify = ip.Address.ToString();
                if(stringify.StartsWith("127.0"))
                    continue;
                
                var encoded = new StringBuilder();

                foreach (string part in stringify.Split('.'))
                {
                    var num = int.Parse(part);
                    encoded.Append(Mapping[num / 10]); 
                    encoded.Append(Mapping[num % 10]); 
                }

                return (stringify, encoded.ToString());
            }

            return (null, null);
        }

        public static bool IsWebSocketRequest(string request, out string websocketKey)
        {
            if (request.Contains("Upgrade: websocket", StringComparison.OrdinalIgnoreCase) &&
                request.Contains("Connection: Upgrade", StringComparison.OrdinalIgnoreCase) &&
                request.Contains("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase) &&
                request.Contains("Sec-WebSocket-Version: 13", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var line in request.Split("\r\n"))
                    if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
                    {
                        websocketKey = line.Split(':')[1].Trim();
                        return true;
                    }
            }
            
            websocketKey = null;
            return false;
        }
        
        public static void ComputeWebsocketAcceptResponse(string websocketKey, out string response)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(websocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
                var hashed = Convert.ToBase64String(hashBytes);
                
                response = "HTTP/1.1 101 Switching Protocols\r\n" +
                           "Upgrade: websocket\r\n" +
                           "Connection: Upgrade\r\n" +
                           $"Sec-WebSocket-Accept: {hashed}\r\n\r\n";
            }
        }
        
        public static string ComputeConnectionHash(IPEndPoint remoteEndPoint)
        {
            return $"{remoteEndPoint.Address}:{remoteEndPoint.Port}";
        }
        
        public static void ReadWebSocketMessage(NetworkStream stream, out byte opCode, out byte[] payload)
        {
            var buffer = new byte[2];
            stream.Read(buffer, 0, 2); 
            
            opCode = (byte)(buffer[0] & 0b00001111);
            
            var isMasked = (buffer[1] & 0b10000000) != 0;
            var payloadLength = buffer[1] & 0b01111111; 

            if (payloadLength == 126)
            {
                buffer = new byte[2];
                stream.Read(buffer, 0, 2);
                payloadLength = BitConverter.ToUInt16(buffer.Reverse().ToArray(), 0);
            }
            else if (payloadLength == 127)
            {
                buffer = new byte[8];
                stream.Read(buffer, 0, 8);
                payloadLength = (int)BitConverter.ToUInt64(buffer.Reverse().ToArray(), 0);
            }

            var maskingKey = new byte[4];
            if (isMasked)
            {
                stream.Read(maskingKey, 0, 4); 
            }

            payload = new byte[payloadLength];
            stream.Read(payload, 0, payloadLength); 

            if (isMasked)
                for (int i = 0; i < payload.Length; i++)
                    payload[i] ^= maskingKey[i % 4];
        }
        
        public static async Task SendWebSocketMessage(NetworkStream stream, string message)
        {
            var payload = Encoding.UTF8.GetBytes(message);
            var payloadLength = payload.Length;

            byte[] frame;
            if (payloadLength <= 125)
            {
                frame = new byte[2 + payloadLength];
                frame[1] = (byte)payloadLength;
            }
            else if (payloadLength <= 65535)
            {
                frame = new byte[4 + payloadLength];
                frame[1] = 126;
                BitConverter.GetBytes((ushort)payloadLength).Reverse().ToArray().CopyTo(frame, 2);
            }
            else
            {
                frame = new byte[10 + payloadLength];
                frame[1] = 127;
                BitConverter.GetBytes((ulong)payloadLength).Reverse().ToArray().CopyTo(frame, 2);
            }

            frame[0] = 0b10000001; 
            Array.Copy(payload, 0, frame, frame.Length - payloadLength, payloadLength);

            await stream.WriteAsync(frame, 0, frame.Length);
            await stream.FlushAsync();
        }

        public static ControllerData Deserialize(int offset, in byte[] data)
        {
            float joyStickAx = BitConverter.ToSingle(data, offset + 4);
            float joyStickAy = BitConverter.ToSingle(data, offset+ 8);
            
            float joyStickBx = BitConverter.ToSingle(data, offset + 12);
            float joyStickBy = BitConverter.ToSingle(data, offset + 16);

            short letterButtons = BitConverter.ToInt16(data, offset + 20);
            short directionButtons = BitConverter.ToInt16(data, offset + 22);

            return new ControllerData(joyStickAx, joyStickAy, joyStickBx, joyStickBy, letterButtons, directionButtons);
        }
    }
}