# Quantforce provides automatic predictive model

QuantQollect is a subset of Quantforce engine that offer bad payer detection 3 month in advance. This Github provide a commented example on how to use this API.

# How it works?

You should provide a two year invoice file to build the model. This file is composed of these mandatory columns
- Cutomer id (ID) ==> customer
- Invoice id (VID)
- Invoice date (VD)
- Invoide term date (TD) ==> invoice
- Invoice effective payment date (PD)
- Invoie amount (AM) ==> invoice
    
Id are string, date are in format dd/mm/yy or dd/mm/yyyy, amount are decimal with point or coma separator. You can add other columns they will be ignored. Column separator can be , ; or tab, it'll be automatically detected.

For example

`ID,VID,VD,TD,AM,OPEN,AM2,PD
148271,11423,30/12/2011,30/03/2012,1167,0,1167,25/01/2012
148268,128662,30/12/2011,29/02/2012,30824,0,30824,30/01/2012
148268,128853,31/12/2011,01/03/2012,38473,0,38473,30/01/2012`

# Authentication
With your account you get a token (for example `FAE04EC0-301F-11D3-BF4B-00C04F79EFBC`)

This token should be put in each HTTP request header (see Rest.cs file)

`token: FAE04EC0-301F-11D3-BF4B-00C04F79EFBC`

# Node
With this token you have acces to nodes. A node is a generic JSON container with one parent and as many child as you want. This node tree concept allows you to organise as you want. You can save personnal data (metadata) in a node, for example your customer id.

Look at the NodeView.cs file for structure description.

# Project
A project is a node with type = project. A project is the entry point for QuantQollect. You create a project node and attach the CSV file.

# Tasks
Because data processing can take a long time, and connection can always go to timeout we the task notion. Long processing will return a taskId so you can query when the task is finish. 

A task
- Will do some reporting, updating constantly a message about what the process is doing.
- If you set the callback URL the system do a GET on this URL when the task is done.
- You can wait as long as you want about a task, it never goes to timeout.

# File transfert
Because files can be very huge you can split then and send then chunk by chunk. When all chunk are uploaded you call "process" on the file API.

---
Have a look at C# example code for a detailed view on how to use QuantQollect API
