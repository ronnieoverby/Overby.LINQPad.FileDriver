# Overby.LINQPad.FileDriver

## What's it do?

This driver allows you to define a connection and point it to a folder full of data files (csv only for now). A datacontext will be generated so that you can query the files in the folder. Each file in the folder will have a class generated for it. Each property in the generated class will have the type that best fits the data in the file.

> Kind of like an F# type provider? - Michael Ciccotti

Yes. Kind of like that.

## Installation

### LINQPad v6

This driver can be installed via nuget. In LINQPad, click `Add Connection` and then `View more drivers...`.

![LP6 Drivers](https://user-images.githubusercontent.com/101028/79165476-3039d000-7db1-11ea-8753-ea6501897f6c.png)

Check `Include Prerelease` and  `Show all drivers`. Search for `Overby.LINQPad.FileDriver`. Click `Install`.

![LP6 Drivers on nuget](https://user-images.githubusercontent.com/101028/79183707-37c49d80-7de0-11ea-8512-73b18df2a8ec.png)


### LINQPad v5

The driver isn't yet available in the normal distribution channels for Linqpad 5. That will happen soon. For now, go to [this repo's releases page](https://github.com/ronnieoverby/Overby.LINQPad.FileDriver/releases) and download the latest released `.lpx` file.

In LINQPad, click `Add Connection` and then `View more drivers...`.

![image](https://user-images.githubusercontent.com/101028/79184097-38a9ff00-7de1-11ea-86a7-b1af390508bc.png)

Click `Browse...` and select the `.lpx` file.

![image](https://user-images.githubusercontent.com/101028/79184223-8888c600-7de1-11ea-8dbf-b1a85ce7a25c.png)


## Configuration

Once installed, click `Add Connection`. Select `Overby File Driver` and click `Next >`.

![image](https://user-images.githubusercontent.com/101028/79165801-e2719780-7db1-11ea-99e3-a2b620ea5488.png)

The dialog connection will be displayed. Supply a folder and a name for the connection.

![image](https://user-images.githubusercontent.com/101028/79165884-0b922800-7db2-11ea-98fa-697d6261aadc.png)

## Gotchas

- CSV only for now. More file types will be supported.
- The first line of each csv file will be treated as a header. Each value in that line will be the name of a property on the generated class. Support for headerless files will be added. For now just add a header line if it's missing, ya dingus!
- "I don't see my file in the explorer!"
  - Sub directories are not recursed. Ensure the file is in the top level. Recursion support will be added.
  - The file needs a `.csv` extension.
  - Refresh the connection for newly added files.
- "Populating the connection is slow!"
  - Code is being generated and compiled during every connection refresh.
  - Each file's MD5 hash is computed during population.
  - New/changed files are scanned to find best-fitting types for properties.
  - The larger the file, the slower the type scanning.
  - The more files, the slower the hashing.  
  - Mitigations: 
     - Type scanning results are cached.
     - Multiple files are hashed/type scanned in parallel.
 - "What is this disgusting `.5f969db29db8fe4dbd4738bf85c80219` folder!? YUCK!"
   - That's the cache. Don't mess with that.
   - The name was chosen to avoid conflicts.
 - "I found a bug!"
   - Good. [Let me know about it.](https://github.com/ronnieoverby/Overby.LINQPad.FileDriver/issues)
 - "I have an idea!"
   - Good. [Let me know about it.](https://github.com/ronnieoverby/Overby.LINQPad.FileDriver/issues)

## Todo

- Cleanup messy code (I built this thing quickly)
- CI/CD
- Enum support for generated types
- Support more file types
  - tsv
  - any delimiter
  - xls[x]
  - fixed width "flat files"
  - json
  - xml
- Faster code-gen
  - More short-circuiting during type scanning
- Edit/Save support
- Directory recursion
- Make build dependencies available on nuget
- Support CSV files without a header line
- Utilities for easily writing files to the folder
- Auto populate when a query writes to the folder
