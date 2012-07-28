# Mite is a simple sql migration framework

The goal of Mite is to make sql migrations simple and painless without introducing new DSL syntax, file formats or xml for the developer to learn,  
and to make doing migrations easier than not doing migrations for even the most simple project.

### Supported Databases

* Microsoft SQL
* MySql

### Supported Platforms

* Any machine that can run .net or mono applications
* Tested with Mono 2.6.7
* Tested with .Net 4.0

# Tenants of Mite

* SQL is a perfectly good DSL (Domain Specific Language) 
* Developers know and have tools for SQL.
* Down migrations are required for expected behavior on conflict resolution.
* Order is significant in migrations and is enforced.
* Migrations should be language agonostic.  So a utility is appropriate.
* Database consistency with the migrations should be enforced.  We should be aware of altered migrations.

## Getting Started with Mite on a Existing Database
* Create a directory for your sql scripts.  For the purposes of this guide we'll use "scripts".
    `cd scripts`
    `mite init`

* Follow the steps in the wizard. It will guide you through creating the _base.sql and mite.config.
* That's it, see "Creating your first migration".

## Getting Started with Mite on a New Project
* Download mite from the Downloads section of this site.  https://github.com/soitgoes/Mite/downloads
* Add the mite install directory to your PATH.   (C:\Program Files\Mite\)
* `cd scripts`
* `mite init`

## Inheriting A Mite Project
If you are working on an existing project that uses mite and you do not have a database setup yet:

* Make sure that you have an empty database created and are in your scripts directory.  Then execute the following commands.
* `mite init`
* `mite update`

## Creating your first migration
Use the following steps:
* `mite -c`
* Open the file that was created and insert your up and down migration (See video for shortcut using tooling).
* `mite update`  (in order to bring your database current)

## Mite Best Practices
* Use source control.  If you keep your migration scripts in the same repository as your source then you will always have the appropriate migrations in order to make your database current for that version.
* Keep your scripts directory in a place that will not be published in a web accessible directory.  

## Migrating Up & Down
* `mite stepup`  will execute one up migration.
* `mite stepdown`  will execute one down migration.
* `mite -d 2011-01-03`  will execute up or down migrations until it reaches that version exactly or it will execute migrations until it passes that key.
* `mite /?`  will display the help which shows all commands.

## Installation
* Download the distributable or build from source.
* Add the location of Mite.exe to your PATH variable.

## Mono
* Download the mono distributable or build from source with xbuild.
* Use the instructions above but use mite.exe instead of mite.  The mono intrepreter should load it.  If not then prefix it with mono
* Example: `mite.exe update` or `mono mite.exe update`

## The MIT License

Copyright (c) 2011 Whiteboard-IT LLC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.