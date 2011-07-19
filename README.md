Mite is a simple migration framework

The goal of Mite is to make migrations simple and painless without introducing new DSL syntax, file formats or xml for the developer to learn 
and to make doing migrations easier than not doing migrations for even the most simple project.

Supported Databases
MsSql
MySql

Supported Platforms
Any machine that can run .net or mono applications

The tenants of Mite are as follows
* SQL is a perfectly good DSL that developers know and already have tooling for.
* Down migrations are required for expected behavior on conflict resolution.
* Order is significant in migrations and is enforced
* Migrations should be language agonostic.  So a utility should be just fine.
* Database consistency with the migrations should be enforced.  If migrations are altered we should know about it.

# Getting Started with Mite on a Existing Database
* Create a directory for your sql scripts.  For the purposes of this guide we'll use "scripts".
* cd scripts
* mite init
* follow the steps on the wizard which will guide you through creating your _base.sql and creating your mite.config
# That's it, see "Creating your first migration"

# Getting Started with Mite on a New Project
* cd scripts
* mite init

# Inheriting A Mite Project
*If you are working on an existing project that uses mite and you do not have a database setup yet*
* make sure that you have an empty database created and that are in your scripts directory
* mite init
* mite update

# Creating your first migration
* execute "mite -c" 
* open the file that was created and insert your up and down migration (See video for shortcut using tooling)
* execute "mite update" in order to bring your database current

# Mite Best Practices
* Use source control.  If you keep your migration scripts in the same repository as your source then you will always have the appropriate migrations in order to make your database current for that version.
* Keep your scripts directory in a place that will not be published in a web accessible directory.  

# Migrating Up & Down
* "mite stepup" will execute one up migration
* "mite stepdown" will execute one down migration
* "mite -d 2011-01-03" will execute up or down migrations until it reaches that version exactly or it will execute migrations until it passes that key
* "mite /?" will bring up the help which list all commands.

The MIT License

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