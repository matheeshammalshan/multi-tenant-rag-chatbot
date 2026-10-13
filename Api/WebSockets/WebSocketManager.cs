using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Api.WebSockets
{
    public static class WebSocketManager
    {
        private static List<WebSocket> _sockets = new List<WebSocket>();

        public static void AddSocket(WebSocket socket)
        {
            _sockets.Add(socket);
        }

        public static async Task SendMessageToAll(DocumentResponseDto responseDto)
        {
            string json = JsonSerializer.Serialize(responseDto);
        
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            foreach (var socket in _sockets.ToList()) 
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
                else
                {
                    _sockets.Remove(socket); 
                }
            }
        }
    }
}