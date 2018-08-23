# Tische

Tische executes SQL queries and renders the result as a PDF using XSL-FO.
Currently, only MS SQL Server is supported.

See `config-sample.xml` for a sample configuration file.  You must edit at
least `<connection>`, `<query>` and `<columns>`.  

```
PS> tische config.xml
```

Currently no binary distribution is available.  Build using Visual Studio or MSBuild;
building the Installer project will create an NSIS-based installer executable.

## License

Tische is copyright (c) 2018 SiKol Ltd.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely. This software is provided 'as-is', without any express or implied
warranty.
