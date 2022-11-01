1. Konzolová aplikace sloužící k převodu zazipovaných seznamů neplatných dokladů ve formátů CSV uložených na stránkách ministerstva do tabulky v crm dynamics 365.
2. Import probíhá pomocí WebAPI.
3. Obsahuje zdrojový kód + instalační soubor.
4. Po prvním spuštění programu (nebo instalaci) se ve složce "AppData\MS\Import Invalid Documents" vytvoří textový soubor s parametry importu. Mezi ně patří přihlašovací údaje pro přístup do CRM, počet maximálních paralelních požadavků na server (zápis, přepis a vymazání dokladu z tabulky), základní adresa webové služby WebAPI a jestli má dojít k importu nebo promazání dat.
5. Každý doklad obsahuje číslo dokladu, může anebo nemusí obsahovat sériové číslo a datum zneplatnění. Každý typ dokladu má v CRM přiřazený svůj kód (OP bez série-805210000; OP se sérií-805210001, Cestovní pas fialový-805210002).
6. U každého inportovaného typu dokladu se ještě zvlášť nastavuje jeho kódové označení v CRM, jestli obsahuje sérii a jestli se má importovat.
7. Po každém importu se ve stejné složce, kde je uložen textový soubor s nastaveními, vytvoří složky s reporty importu, které se pojmenují podle data importu a kódového označení dokladu.
