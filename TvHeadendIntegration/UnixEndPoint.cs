﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TvHeadendIntegration
{
    [Serializable]
    public class UnixEndPoint : EndPoint
    {
        readonly string _filename;

        public UnixEndPoint(string filename)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            _filename = filename;
        }

        public override AddressFamily AddressFamily => AddressFamily.Unix;

        public override EndPoint Create(SocketAddress socketAddress)
        {
            /*
			 * Should also check this
			 *
			int addr = (int) AddressFamily.Unix;
			if (socketAddress [0] != (addr & 0xFF))
				throw new ArgumentException ("socketAddress is not a unix socket address.");
			if (socketAddress [1] != ((addr & 0xFF00) >> 8))
				throw new ArgumentException ("socketAddress is not a unix socket address.");
			 */

            if (socketAddress.Size == 2)
            {
                // Empty filename.
                // Probably from RemoteEndPoint which on linux does not return the file name.
                return new UnixEndPoint(string.Empty);
            }
            var size = socketAddress.Size - 2;
            var bytes = new byte[size];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = socketAddress[i + 2];
                // There may be junk after the null terminator, so ignore it all.
                if (bytes[i] == 0)
                {
                    size = i;
                    break;
                }
            }

            var name = Encoding.Default.GetString(bytes, 0, size);
            return new UnixEndPoint(name);
        }

        public override SocketAddress Serialize()
        {
            var bytes = Encoding.Default.GetBytes(_filename);
            var sa = new SocketAddress(AddressFamily, 2 + bytes.Length + 1);
            // sa [0] -> family low byte, sa [1] -> family high byte
            for (var i = 0; i < bytes.Length; i++)
                sa[2 + i] = bytes[i];

            //NULL suffix for non-abstract path
            sa[2 + bytes.Length] = 0;

            return sa;
        }

        public override string ToString()
        {
            return (_filename);
        }

        public override int GetHashCode()
        {
            return _filename.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as UnixEndPoint;
            if (other == null)
                return false;

            return (other._filename == _filename);
        }
    }
}