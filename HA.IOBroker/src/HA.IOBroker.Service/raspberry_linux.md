Install git on raspberry pi (pi4a)
  sudo apt update
  sudo apt upgrade
  sudo apt install git

Create git folder and change owner
  sudo mkdir /git
  sudo chown thomas /git

Create a ssh key for github
  ssh-keygen -t ed25519 -C "tkdetrial@gmail.com"
  cd ~/.ssh/
  cat id_ed25519.pub

Clone github repo
  git clone git@github.com:ThomasKeller/HomeAutomation.git

Install dotnet LTS version (6) => stored in ~/.dotnet
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel LTS
  echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
  echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
  source ~/.bashrc

Compile Kostal Service
  cd /git/HomeAutomation/ha.kostal.service/
  dotnet build
  dotnet run
  dotnet publish -o /git/relases/kostal.service -c Release --self-contained --use-current-runtime
   
Configure Kostal Service
  mkdir /git/releases/kostal.service/store
  mkdir /git/releases/kostal.service/settings
  nano /git/releases/kostal.service/settings/appsettings.json => add configuration
   
Add Kostal Service to /etc/systemd/system/
  sudo nano /etc/systemd/system/kostal.service
	[Unit]
	Description=Kostal Service (.NET)
	
	[Service]
	ExecStart=/git/releases/kostal.service/ha.kostal.service
	WorkingDirectory=/git/releases/kostal.service
	User=thomas
	Group=thomas
	
	[Install]
	WantedBy=multi-user.target
 
  sudo systemctl start kostal.service
  sudo systemctl status kostal.service
  sudo systemctl enable kostal.service
  
Show Log for Kostal Service 
  sudo journalctl -u kostal.service -b
  sudo journalctl -u kostal.service



   

   
