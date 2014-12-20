using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Extensions
{
    public static class SocketExtensions
    {
        /// <summary>
        /// Asynchronously receive data from a connected <see cref="Socket"/>.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="buffer">An array of type <see cref="byte" /> that is the storage location for the received data.</param>
        /// <param name="offset">The zero-based position in the <paramref name="buffer" /> parameter at which to store the received data.</param>
        /// <param name="size">The number of bytes to receive.</param>
        /// <param name="socketFlags">A bitwise combination of the <see cref="SocketFlags" /> values.</param>
        /// <returns>A task returning the number of bytes recieved.</returns>
        public static async Task<int> ReceiveAsync(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            return await Task.Factory.FromAsync<int>(
                socket.BeginReceive(buffer, offset, size, socketFlags, null, socket),
                socket.EndReceive);
        }

        /// <summary>
        /// Asynchronously establishes a connection to a remote host.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="remoteEP">An <see cref="EndPoint"/> that represents the remote device.</param>
        /// <returns>The task.</returns>
        public static async Task ConnectAsync(this Socket socket, EndPoint remoteEP)
        {
            await Task.Factory.FromAsync(
                socket.BeginConnect(remoteEP, null, socket),
                socket.EndConnect);
        }

        /// <summary>
        /// Asynchronously establishes a connection to a remote host.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="buffer">An array of type <see cref="byte" /> that contains the data to be sent.</param>
        /// <param name="offset">The position in the data buffer at which to begin sending data.</param>
        /// <param name="size">The number of bytes to send. </param>
        /// <param name="socketFlags">A bitwise combination of the <see cref="SocketFlags" /> values.</param>
        /// <returns>The task returning the number of bytes sent to the <see cref="Socket"/>.</returns>
        public static async Task<int> SendAsync(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            return await Task.Factory.FromAsync<int>(
                socket.BeginSend(buffer, offset, size, socketFlags, null, socket),
                socket.EndSend);
        }

        /// <summary>
        /// Asynchronously establishes a connection to a remote host.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="buffer">An array of type <see cref="byte" /> that contains the data to be sent.</param>
        /// <param name="offset">The position in the data buffer at which to begin sending data.</param>
        /// <param name="size">The number of bytes to send. </param>
        /// <param name="socketFlags">A bitwise combination of the <see cref="SocketFlags" /> values.</param>
        /// <param name="remoteEP">The <see cref="EndPoint"/> that represents the destination for the data.</param>
        /// <returns>The task returning the number of bytes sent to the <see cref="Socket"/>.</returns>
        public static async Task<int> SendToAsync(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP)
        {
            return await Task.Factory.FromAsync<int>(
                socket.BeginSendTo(buffer, offset, size, socketFlags, remoteEP, null, socket),
                socket.EndSend);
        }
    }
}