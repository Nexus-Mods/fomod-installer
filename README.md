# FOMOD Installer Core

Cross platform stripped version of FOMOD, used for bundling game mods.  
Used in the [Nexus Mods App](https://github.com/Nexus-Mods/NexusMods.App), and friends.  

## Deployment

New versions of packages in this branches are pushed to NuGet when a `Tag` is uploaded; with corresponding GitHub releases made.  

## Setting up Nuget Publishing

If you fork this repository; you will need to set up keys if you wish to publish packages from CI/CD.  

You can set your key in  
- `Settings` -> 
- `Settings and Variables` -> 
- `Actions` -> 
- `New Repository Secret` -> 
- Set secret for NUGET_KEY.

To generate a NuGet.org API key, visit [API Keys](https://www.nuget.org/account/apikeys).  
If you wish to change publish settings, see [CI/CD Config](./.github/workflows/build-and-publish.yml).  