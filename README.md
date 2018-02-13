# durable-functions
Durable Functions is a simplified code I am working on to test durable functions as serverless actors. 

This brief test turned up the following comments and anamolies:

### .NET Core and .NET Standard 2.0

It is work in progress! It is best to use the .NET full framework with Azure Durable Functions. Hopefully this will change soon and we will be able to use .NET Core reliably.

### Local Debugging

I had a very hard time with this. The symptoms that I experienced are unfortutanely not experienced by other developers who tried this as I could not see similar reported issues. I am using Vs2017 and Azure Functions Extension ...the latest at the time of writing JAN/2018. 

Here are my notes:

- If you want to debug locally, make sure you set both the local.setting.json and host.json file to `copy always`. You do this from the properties window.
- On both of my developer machines, I hit F5, it prompts me to install the Azure Functions Core tools and things look quite good. I was able to run locally.
- But then subsequent F5, I get very different results ranging from:
    - The CLI starts and exits on its own ...I could not find out what the reason is
    - The CLI starts and displays the functions URls. But it also complains about some files were changed and the host needs to restart. The URls are not responsive and there is nothing you can do except to terminate and restart.
    - The CLI statrs and actually works.....does not happen often ...but I have seen it work
    - F5 mostly causes the CLI to start and exit. Control-F5 does not exit ...but the function URLs are not accessible due to this `change detected` message.
- Effectively, local debugging did not work for me at all. It was a very frustrating experience. So I had to deploy everything (see deplyment below) to Azure and debug there....another frustrating experience. 

### Deployment

- The only effective way I was able to find out how to deploy a Durable Functions App is via Visual Studio. I have heard some people got it to work with VSTS. But, given that this is a test run, I did not really explore this option.
- However, if you just click the `publish` button in VS, it will auto-create a storage account for you which names things really weird. My recommendation is to create the App Service, Service Plan, Storage and App Insights in portal or via Azure CLI and then use Visual Studio to publish into it. 
- If you renamed a function and re-deployed, Visual Studio will publish the new functions app with the new function. But the old function will still be there (you can see it from the portal). You can then use `Kudo`, navigate to the directory and actually delete the old function folder. 
- The local.settings.json entries are not deployed to Azure! This means you have to manually create them in the portal app settings or in Visual Studio deployment window. 

### Storage

As mentioned, an Azure Storage is required to maintain teh durable instances. They are keyed off the hub name you specify in the host. There are entries in blob, tables, files and queues. 

### Logging

Unless you turn on streaming on a function in the portal, you don't get anything (or at least, I could not find a way to do it). But watching the log from the portal is very difficult as it times out and it is not very user friendly. This is one area that requires better UX in the portal. The logs are also stored on the app's file system which you can access from `Kudo`. However, I noticed that, unless you request stream logging on a function, these files are not created. 

So the story of logging is a little frustrating at best! I had to use App Insights trace to see what is going on.

### Timers
 
As mentioned above, it is very important that we leverage the context to provide accurate timer information as opposed to `TimeSpan` and `DateTime.Now`, etc. Initially I used `TimeSpan.FromMinutes(30)` to wait for 30 minutes....but the way to do it is to always use the `context` such as `DateTime deadline = context.CurrentUtcDateTime.AddMinutes(30);`. After doing that, I started getting more consistent timeout periods when the timeout period was small i.e. 5 minutes. However, when I made the timeout periods longer i.e. 30 minutes, ran 2-3 actors and actually had the actor call a real function that calls a search service (omitted from this repository code), the actor did not wake up consistently! It is very difficult to re-produce or prove that this was actually happening....but you kind have to take my word for it :-)       

### Instance Termination

Although `TerminateAsync` on an instance works, I am not exactly sure if it works the way it is supposed to:

- If I have a running instance and that instance is actually waiting on an external or time out event, `TerminateAsync` does not do anything. I guess because a message is enqueued to the instance but the instance is waiting on other events ....so it did not get the `terminate` signal yet.
- If the instance is not waiting on anything, `TerminateAsync` replays the instance which runs code that you don't necessarily want to run. For example, I had an instance that triggers a logic app once it receives an `end` operation which works. However, if I terminate the instance using `TerminateAync`, the code triggers the logic app again because it was replayed! 

Not sure if this behavior is correct and what the `terminate` signal actually do.       

 
