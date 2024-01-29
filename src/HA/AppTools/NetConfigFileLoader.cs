using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HA.AppTools;

internal class NetConfigFileLoader
{
	private readonly NetworkCredential _networkCredential;

	public NetConfigFileLoader(string netPath, string user, string password)
	{
		_networkCredential = new NetworkCredential(user, password);




	}


}
