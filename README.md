# Tische

Tische executes SQL queries and renders the result as a PDF using XSL-FO.
Currently, only MS SQL Server is supported.

See `config-sample.xml` for a sample configuration file.  You must edit at
least `<connection>`, `<query>` and `<columns>`.  

Run Tische and provide your configuration file:

```
PS> tische config.xml
```

An (unsigned) installer and portable executable are available from the GitHub
'Releases' page. 

Please report any issues to ft@le-fay.org.

## License

Tische is copyright (c) 2018 SiKol Ltd.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely. This software is provided 'as-is', without any express or implied
warranty.
