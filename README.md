# tuya-home
An experimental program to manage tuya devices on your local network without the cloud.

# Supported Protocols
- Tuya 3.1 protocol 
- Tuya 3.3 protocol

# Features
- Automatically add all devices linked to your Tuya developer account, and find their IPs via mac matching from resolved hosts on your ARP table
- Support for some basic devices with custom controls
- Debugging devices real-time
- Mass-control all devices
- Automatically find the dps entry for controlling device power (you won't need to implement your device if it's not officially supported)
- Simplistic login with details remembered
- Brute force hidden DPS inputs
- Automatic icon downloading from Tuya server

# To do
- Add support for Tuya 3.4 protocol
- Cleanup UI
- Develop dynamic link library for creation of custom devices without the need to recompile the program
- Re-write some source due to the shoddy method of handling different devices
