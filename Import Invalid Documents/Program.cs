using Import_Invalid_Documents;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

//Vytvoří instanci importeru, kde se nastaví přihlašovací údaje k CRM, počet maximálních požadavků na server a adresa API rozhraní
CsvImporter csvImporter = new CsvImporter(Settings.GetUserName(), Settings.GetPassword(), Settings.GetParralelTasks(), Settings.GetBaseUrl());

//Nejprve ověří, jestli má dojít k importu dat a pokud ano, tak dojde k nastavení cyklu pro všechny importy, kterým se nastaví adresa, kde jsou
//uloženy soubory, typ dokladu a jestli doklad obsahuje číslo série a zvlášť pro každý import se ještě ověří, jestli se má importovat.
if (Settings.Import())
{
    foreach (var import in Settings.GetDownloadParameters().AsArray())
    {
        if (!bool.Parse(import["Import"].ToString()))
            continue;
        csvImporter.Import(import["url"].ToString(), int.Parse(import["AcmType"].ToString()), bool.Parse(import["AcmBatch"].ToString()));
    }
}

//Ověří zda se má tabulka neplatných dokladů promazat a pokud ano, tak se promaže
if (Settings.Delete())
{
    csvImporter.ClearCRM();
}

Console.WriteLine("Konec importu/mazání dat.");
