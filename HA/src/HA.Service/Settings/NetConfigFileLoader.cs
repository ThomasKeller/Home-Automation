using System.Net;

namespace HA.Service.Settings;

internal class NetConfigFileLoader
{
	private readonly NetworkCredential _networkCredential;

	public NetConfigFileLoader(string netPath, string user, string password)
	{
		_networkCredential = new NetworkCredential(user, password);




	}


}
