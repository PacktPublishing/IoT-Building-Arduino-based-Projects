using System;
using Clayster.Library.Data;

namespace Clayster.Library.IoT.Provisioning
{
	/// <summary>
	/// Contains cached responses from a provisioning server.
	/// </summary>
	/// <remarks>
	/// © Clayster, 2014
	/// 
	/// Author: Peter Waher
	/// </remarks>
	[CLSCompliant (true)]
	[Serializable]
	[DBDedicatedTable]
	public class CacheItem : DBObject
	{
		private string hash = string.Empty;
		private string server = string.Empty;
		private string xmlResponse = string.Empty;
		private DateTime lastAccess = DateTime.Now;

		/// <summary>
		/// Contains cached responses from a provisioning server.
		/// </summary>
		public CacheItem ()
			: base (ProvisioningServer.db)
		{
		}

		/// <summary>
		/// Contains cached responses from a provisioning server.
		/// </summary>
		/// <param name="Server">Provisioning Server address.</param>
		/// <param name="Hash">Hash of the request.</param>
		/// <param name="XmlResponse">XML Response.</param>
		public CacheItem (string Server, string Hash, string XmlResponse)
			: base (ProvisioningServer.db)
		{
			this.server = Server;
			this.hash = Hash;
			this.xmlResponse = XmlResponse;
		}

		/// <summary>
		/// Provisioning Server address.
		/// </summary>
		[DBShortString (true)]
		public string Server
		{
			get
			{
				return this.server; 
			}

			set
			{
				if (this.server != value)
				{
					this.server = value;
					this.Modified = true;
				}
			}
		}

		/// <summary>
		/// Hash of the request.
		/// </summary>
		[DBShortString (false)]
		public string Hash
		{
			get
			{
				return this.hash; 
			}

			set
			{
				if (this.hash != value)
				{
					this.hash = value;
					this.Modified = true;
				}
			}
		}

		/// <summary>
		/// XML Response
		/// </summary>
		[DBLongString]
		public string XmlResponse
		{
			get
			{
				return this.xmlResponse; 
			}

			set
			{
				if (this.xmlResponse != value)
				{
					this.xmlResponse = value;
					this.Modified = true;
				}
			}
		}

		/// <summary>
		/// Last access
		/// </summary>
		public DateTime LastAccess
		{
			get
			{
				return this.lastAccess; 
			}

			set
			{
				if (this.lastAccess != value)
				{
					this.lastAccess = value;
					this.Modified = true;
				}
			}
		}

	}
}
