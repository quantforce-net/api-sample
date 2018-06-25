# Quantforce provides automatic predictive model

QuantQollect is a subset of Quantforce engine that offer bad payer detection 3 month in advance. This Github provide a commented example on how to use this API.

# How it works?

You should provide a two year invoice file to build the model. This file is composed of these mandatory columns
- Cutomer id (ID)
- Invoice id (VID)
- Invoice date (VD)
- Invoide term date (TD)
- Invoice effective payment date (PD)
- Invoie amount (AM)
    
Id are string, date are in format dd/mm/yy or dd/mm/yyyy, amount are decimal with point or coma separator. You can add other columns they will be ignore. You cna use , ; or tab for column separator.

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
With this token you have acces to a node that can contains as many child as you want.
