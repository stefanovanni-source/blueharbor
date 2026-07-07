# BlueHarbor — Registro Operativo Terminal

Applicazione web interna per il coordinamento di un piccolo terminal container fittizio.
Progetto *ITS Learning by Project 2025–2027*. Tutti i dati e le regole sono didattici.

---

## Requisiti del progetto

L'esercizio richiede un gestionale web interno per un terminal container, con **due
ruoli operativi distinti** e un **tempo virtuale** condiviso. I requisiti funzionali:

1. **Autenticazione a ruoli** — accesso tramite login; due ruoli con permessi separati:
   - **Operatore**: registra e mantiene le navi in arrivo.
   - **Scheduler**: assegna le navi alle banchine.
2. **Banchine (berth) fisse** — un insieme predefinito e non modificabile: **1 XL, 1 L, 2 M, 4 S**.
   Ogni banchina ha una dimensione.
3. **Navi (ship)** — ogni nave ha una dimensione (`XL/L/M/S`), un giorno di arrivo, una durata
   di occupazione e uno stato (`PENDING → ASSIGNED → DEPARTED`).
4. **Registrazione nave (Operatore)** — la nave viene creata con **dimensione, giorno di arrivo
   (entro 30 giorni) e durata (3–15 giorni) generati casualmente**; stato iniziale `PENDING`.
5. **Assegnazione (Scheduler)** — lo Scheduler sceglie una **banchina compatibile per dimensione**;
   il sistema calcola il **primo slot temporale libero** a partire dal giorno di arrivo, rispettando
   le occupazioni già presenti; la nave passa a `ASSIGNED`.
6. **Tempo virtuale + Next Day** — esiste un orologio a giorni interi; l'azione *Next Day* avanza il
   giorno di 1 e porta a `DEPARTED` le navi la cui finestra di occupazione è terminata.
7. **Fuori scope** — nessuna pianificazione/ottimizzazione automatica, nessun KPI, nessun real-time,
   nessuna modifica o riassegnazione dopo l'assegnazione.

## Come i requisiti sono stati soddisfatti

| # | Requisito | Implementazione |
|---|-----------|-----------------|
| 1 | Autenticazione a ruoli | **Cookie authentication** (`Program.cs`) + form login (`HomeController.Login`). Due utenti demo e i ruoli `OPERATORE`/`SCHEDULER` in `Security/InMemoryUserStore.cs`. Gli endpoint sono protetti con `[Authorize(Roles = ...)]` (`ShipController` → Operatore, `SchedulerController` → Scheduler) e le viste mostrano le azioni per ruolo con `User.IsInRole`. |
| 2 | Banchine fisse | Seed all'avvio in `Data/DataInitializer.cs` (1 XL, 1 L, 2 M, 4 S). Entity `Berth` in `Domain/`, `Code` univoco. |
| 3 | Modello Ship | Entity `Ship` + enum `Size` e `ShipStatus` in `Domain/`. Schema configurato in `Data/AppDbContext.cs`. |
| 4 | Registrazione nave | `ShipService.Create` genera casualmente dimensione, giorno di arrivo (≤ giorno corrente + 30) e durata (3–15); stato iniziale `PENDING`. Form: `ShipController.NewForm/Create` → viste in `Views/Ship/`. |
| 5 | Assegnazione | `SchedulingService.Assign` valida la compatibilità di dimensione e calcola lo slot con `FirstAvailableStart`; salva un `Assignment` e porta la nave a `ASSIGNED`. UI: `SchedulerController.Board/Assign` → `Views/Scheduler/Board.cshtml`. |
| 6 | Tempo virtuale + Next Day | `TerminalClockService` gestisce l'orologio (`TerminalClock`, riga singola) e l'azione `NextDay`, che imposta `DEPARTED`. Il giorno corrente è iniettato nel layout condiviso (`@inject TerminalClockService`). Azione condivisa `HomeController.NextDay`. |
| 7 | Fuori scope | Nessuna logica di ottimizzazione/KPI/real-time; l'assegnazione è manuale e non riassegnabile (vincolo 1:1 `Assignment`→`Ship` in `AppDbContext`). |

> **Modello a 3 livelli**: `Controllers` (MVC + viste Razor) → `Services` (regole di dominio) →
> `AppDbContext` (EF Core InMemory). Vedi *Architettura* più sotto.

---

## Come avviare

L'app è raggiungibile su **<http://localhost:8080>** (redirige al login). Scegli uno dei metodi.

### Utenti demo
| Utente | Password | Ruolo |
|-----------|-----------|------------|
| `operatore` | `operatore` | Operatore (registra e gestisce le navi) |
| `scheduler` | `scheduler` | Scheduler (assegna le navi alle banchine) |

### A) Da Visual Studio

Prerequisiti: **Visual Studio 2022 (17.8+)** con il workload *ASP.NET and web development*
(include il **.NET 8 SDK**).

1. Apri il progetto: `File ▸ Open ▸ Project/Solution` e seleziona **`BlueHarbor.csproj`**
   (in alternativa `File ▸ Open ▸ Folder…` sulla cartella del progetto).
2. Assicurati che il profilo di avvio selezionato sia **`BlueHarbor`** (definito in
   `Properties/launchSettings.json`: URL `http://localhost:8080`, ambiente `Development`).
3. Premi **F5** (debug) o **Ctrl+F5** (senza debug). Il browser si apre automaticamente
   sull'app.

> In alternativa, con il solo **.NET 8 SDK** e senza IDE, dalla cartella del progetto:
> ```bash
> dotnet run
> ```

### B) Da Docker (senza installare il .NET SDK)

Prerequisito unico: **Docker Desktop / Docker Engine** attivo. La compilazione avviene
dentro il container (build multi-stage nel `Dockerfile`). Guida estesa in **[DOCKER.md](DOCKER.md)**.

Con Docker Compose (consigliato), dalla cartella che contiene il `Dockerfile`:

```bash
docker compose up --build          # in foreground
docker compose up --build -d       # in background
docker compose logs -f             # segue i log
docker compose down                # ferma e rimuove
```

Oppure con Docker "puro":

```bash
docker build -t blueharbor .
docker run --rm -p 8080:8080 blueharbor
```

> **Porta occupata?** Rimappa, es. `-p 9090:8080` (poi apri <http://localhost:9090>) o modifica
> `ports` in `docker-compose.yml`.

### Link utili
- App: <http://localhost:8080>
- Swagger UI (endpoint tecnico di status `/api/status`): <http://localhost:8080/swagger>

---

## Architettura complessiva

Applicazione **ASP.NET Core 8 (MVC)** monolitica a tre livelli, con frontend
server-side reso da **Razor** (pattern MVC classico, nessuna SPA).

```
Browser ──HTTP──> Controller ──> Service ──> AppDbContext (EF Core) ──> DB in-memory
                       │
                   Razor (views) + Cookie Authentication (ruoli)
```

- **Persistenza**: Entity Framework Core con provider **InMemory** (temporaneo, riparte
  vuoto e viene ripopolato all'avvio con banchine e orologio).
- **Sicurezza**: Cookie authentication con form login e due utenti in-memory, uno per ruolo.
- **Presentazione**: Razor views con rendering per ruolo (`User.IsInRole`).

## Componenti principali e responsabilità

| Cartella | Componente | Responsabilità |
|---------|------------|----------------|
| `Domain` | `Ship`, `Berth`, `Assignment`, `TerminalClock` + enum `Size`, `ShipStatus` | Modello dati (entity EF Core) |
| `Data` | `AppDbContext` | Configurazione schema e accesso ai dati |
| `Data` | `DataInitializer` | Seed: orologio (giorno 1) + banchine fisse |
| `Services` | `ShipService` | Creazione/manutenzione navi (Operatore) |
| `Services` | `SchedulingService` | Board banchine + assegnazione (primo slot libero) |
| `Services` | `TerminalClockService` | Tempo virtuale, azione *Next Day*, transizione a *Departed* |
| `Controllers` | `HomeController`, `ShipController`, `SchedulerController` | Endpoint MVC + viste |
| `Controllers` | `StatusController` | Endpoint tecnico `/api/status` (Swagger) |
| `Security` | `InMemoryUserStore`, `Roles` | Utenti demo e ruoli |
| `Program.cs` | Composition root | DI, autenticazione, autorizzazione, routing, seed |

> **Convenzione viste**: la cartella delle viste di un controller deve combaciare col nome del
> controller senza il suffisso `Controller` (es. `ShipController` → `Views/Ship/`), altrimenti
> il view engine non trova la view e solleva `ViewEngineResult.EnsureSuccessful`.

Il giorno virtuale corrente è esposto a tutte le viste tramite `@inject TerminalClockService`
nel layout condiviso (`Views/Shared/_Layout.cshtml`).

## Modello dati (alto livello)

```
Berth (banchina)            Ship (nave)                    Assignment (assegnazione)
------------------          --------------------           --------------------------
Id                          Id                             Id
Code   (es. "M1")           Name, Notes                    ShipId  ─FK(1:1)→ Ship
Size   {XL,L,M,S}           Size {XL,L,M,S}               BerthId ─FK(N:1)→ Berth
                            ArrivalDay                     StartDay
                            OccupationDuration             EndDay  (esclusivo)
                            Status {PENDING,ASSIGNED,DEPARTED}

TerminalClock: Id=1, CurrentDay   (singola riga, orologio virtuale)
```

Occupazione di una banchina: giorni `[StartDay, EndDay)`. La banchina torna libera il
giorno `EndDay`.

Insieme fisso di banchine: **1 XL, 1 L, 2 M, 4 S**.

## Come sono state progettate le tabelle (e perché)

Questa sezione spiega, passo passo, come siamo arrivati a queste tabelle. È pensata
per chi sta imparando: partiamo dalle "cose" del problema e vediamo perché diventano
tabelle e colonne.

### Idea di base: una tabella per ogni "cosa" del problema
Nel racconto del terminal compaiono alcune entità concrete: le **navi**, le **banchine**
e un **orologio** che segna il giorno. Ognuna di queste diventa una tabella. In più c'è
un'azione importante — "questa nave sta a questa banchina in questi giorni" — che diventa
anch'essa una tabella: l'**assegnazione**. Regola pratica: *un sostantivo del dominio →
una tabella; una riga → un singolo oggetto reale*.

### La colonna `Id`: la chiave primaria
Ogni tabella ha una colonna `Id` numerica che identifica in modo univoco la riga
(la **chiave primaria**). Serve per potersi riferire a una riga senza ambiguità: due navi
possono avere lo stesso nome, ma mai lo stesso `Id`. EF Core riconosce la proprietà
`Id` come chiave e la genera in automatico.

### Tabella `Ship` (nave)
```
Ship
-----
Id                  chiave primaria
Name, Notes         inseriti dall'Operatore (Notes è opzionale, max 1000 caratteri)
Size                dimensione: XL / L / M / S
ArrivalDay          giorno di arrivo (numero intero)
OccupationDuration  quanti giorni occuperà la banchina (3..15)
Status              PENDING → ASSIGNED → DEPARTED
```
Perché così: raccoglie in un unico posto tutti i dati che descrivono *la nave in sé*.
Notare che **non** contiene "a quale banchina è assegnata": quella è un'informazione che
riguarda la *relazione* nave–banchina, quindi la mettiamo in un'altra tabella (vedi sotto).

### Tabella `Berth` (banchina)
```
Berth
-----
Id      chiave primaria
Code    codice leggibile es. "M2" (deve essere unico)
Size    dimensione della banchina
```
Le banchine sono **fisse** (1 XL, 1 L, 2 M, 4 S) e vengono create una sola volta all'avvio.
Su `Code` mettiamo un **vincolo di unicità**: impedisce di creare per errore due banchine con
lo stesso codice.

### `Size` e `Status`: perché sono enum e non testo libero
Una nave/banchina può avere solo poche dimensioni (`XL, L, M, S`) e una nave solo pochi stati
(`PENDING, ASSIGNED, DEPARTED`). Usiamo degli **enum** (elenchi chiusi di valori) invece di
stringhe libere: così è impossibile scrivere "Media" o "grande" per sbaglio. Nel database
vengono salvati come testo leggibile (`HasConversion<string>()` in `AppDbContext`), così la
tabella resta comprensibile anche a occhio.

### Tabella `Assignment` (assegnazione): il cuore del progetto
Qui sta la decisione di progettazione più interessante. "Assegnare una nave a una banchina per
alcuni giorni" mette in relazione **due** tabelle e ha dei dati propri (le date). Avevamo due
strade:

- **A)** aggiungere colonne dentro `Ship` (es. `BerthId`, `StartDay`, `EndDay`);
- **B)** creare una tabella dedicata `Assignment`. ← **scelta adottata**

```
Assignment
-----------
Id        chiave primaria
ShipId    → punta a Ship  (chiave esterna)
BerthId   → punta a Berth (chiave esterna)
StartDay  primo giorno di occupazione (incluso)
EndDay    primo giorno di nuovo libero (escluso)  ->  EndDay = StartDay + durata
```

Perché la tabella dedicata (B):
1. **Storico per banchina.** Una banchina nel tempo ospita *molte* navi. Con una tabella a parte
   ogni occupazione è una riga: possiamo conservarne quante ne vogliamo e calcolare il "primo slot
   libero". Con l'opzione A la banchina "ricorderebbe" solo l'ultima nave.
2. **Le date appartengono alla relazione, non alla nave.** `StartDay`/`EndDay` esistono solo
   *perché* c'è un abbinamento nave–banchina: è naturale che stiano nella tabella che rappresenta
   quell'abbinamento.
3. **Separazione pulita.** `Ship` resta la descrizione della nave; `Assignment` descrive "chi sta
   dove e quando".

### Le chiavi esterne (foreign key): come si collegano le tabelle
`Assignment` non ripete nome nave e codice banchina: memorizza solo i loro `Id`
(`ShipId`, `BerthId`). Sono **chiavi esterne**, cioè riferimenti ad altre tabelle. Vantaggio:
se un dato della nave cambia, sta scritto in un solo posto. Le due relazioni hanno
"cardinalità" diverse, ed è voluto:

- `ShipId` è **1:1** (unico): una nave ha **al massimo una** assegnazione. Lo imponiamo con un
  indice unico su `ShipId` in `AppDbContext` — è anche il modo in cui il progetto realizza il
  requisito "niente riassegnazioni".
- `BerthId` è **N:1**: una banchina compare in **molte** assegnazioni (una per ogni nave ospitata
  nel tempo).

### Perché i giorni sono numeri interi
Il problema parla di "giorni" del terminal, non di date reali (14 marzo, ecc.). Rappresentare il
tempo come **numeri interi** (`ArrivalDay`, `StartDay`, `CurrentDay`) rende tutto più semplice:
"avanti di un giorno" è `+1`, e verificare se due occupazioni si sovrappongono è un semplice
confronto tra numeri.

### `EndDay` "escluso": un dettaglio che semplifica i conti
`EndDay` **non** è l'ultimo giorno occupato, ma il **primo giorno di nuovo libero**. Se una nave
arriva il giorno 5 e resta 3 giorni, occupa i giorni 5, 6, 7 e `EndDay = 8`. Con questa convenzione
la durata è `EndDay - StartDay` e il test di sovrapposizione tra due periodi `[a,b)` e `[c,d)` è
semplicemente `a < d && c < b`. È lo stesso motivo per cui in tanti linguaggi gli intervalli
"finiscono uno oltre".

### Tabella `TerminalClock` (l'orologio): una sola riga
```
TerminalClock
--------------
Id          fissato a 1 (impostato a mano, non generato)
CurrentDay  il giorno virtuale corrente
```
Il terminal ha **un solo** orologio, quindi questa tabella contiene sempre **una sola riga**.
È un modo semplice e comprensibile per salvare uno "stato globale" dell'applicazione usando gli
stessi strumenti (una tabella) del resto del modello, invece di inventare un meccanismo a parte.

## Flussi principali

- **Operatore → crea nave**: il sistema genera dimensione casuale, giorno di arrivo casuale
  (entro 30 giorni dal giorno corrente) e durata casuale (3–15 giorni); stato iniziale `PENDING`.
- **Scheduler → assegna**: sceglie una banchina *compatibile per dimensione*; il sistema calcola
  il **primo slot temporale disponibile** `≥ giorno di arrivo` rispettando le occupazioni esistenti;
  salva l'assegnazione e porta la nave a `ASSIGNED`.
- **Next Day** (azione condivisa): avanza il giorno virtuale di 1 e imposta `DEPARTED` per le navi
  la cui finestra di occupazione è terminata. Nessuna assegnazione automatica.

### Algoritmo "primo slot libero" (`SchedulingService.FirstAvailableStart`)
Partendo da `candidate = arrivalDay`, finché `candidate` si sovrappone a una occupazione
esistente della banchina, si sposta `candidate` alla fine di quella occupazione. Gestisce
correttamente i "buchi" tra occupazioni.

## Decisioni progettuali e compromessi

- **Assignment come entità separata** (anziché campi su `Ship`): rende naturale lo storico delle
  occupazioni per banchina e il calcolo dello slot libero. Due foreign key (1:1 verso `Ship`,
  N:1 verso `Berth`).
- **Tempo come intero** (`CurrentDay`, `ArrivalDay`): il dominio parla di "giorni" senza date reali;
  gli interi semplificano il confronto e l'avanzamento.
- **`EndDay` esclusivo**: semplifica il test di sovrapposizione e la regola "libera dal giorno EndDay".
- **Sicurezza minimale** (utenti in-memory, password in chiaro): la gestione utenti è fuori scope;
  l'obiettivo è solo distinguere i due ruoli. *Assunzione ammessa dalla traccia.*
- **Next Day accessibile a entrambi i ruoli**: il tempo è una risorsa condivisa del terminal, non
  di un singolo ruolo. *Assunzione documentata.*
- **DB in-memory (EF Core)**: nessun database reale da installare; adatto a un esercizio didattico.
  I dati si azzerano a ogni riavvio e vengono ricreati dal seed.

## Fuori scope (come da traccia)
Nessuna pianificazione/ottimizzazione automatica, nessun KPI, nessun real-time, nessuna
modifica o riassegnazione dopo l'assegnazione.

## Stack tecnico
.NET 8 · ASP.NET Core MVC · Entity Framework Core (InMemory) · Cookie Authentication ·
Razor · Swashbuckle (Swagger).

---

## Evoluzione futura: separare backend e frontend

> Questa sezione è **teorica/di progetto**: descrive come si *potrebbe* evolvere l'app.
> Non è ancora implementata.

### Com'è adesso e cosa vogliamo cambiare
Oggi l'app è **monolitica**: lo stesso programma ASP.NET Core sia contiene la logica
(i dati, le regole) sia costruisce le pagine HTML con Razor. Backend e frontend sono
"cuciti insieme".

Separarli significa avere **due parti indipendenti**:

- il **backend** diventa un'**API**: non produce più pagine HTML, ma risponde con **dati in
  formato JSON** (es. l'elenco delle navi come lista di oggetti);
- il **frontend** diventa un insieme di file **HTML + JavaScript** a sé stante, che *chiede*
  i dati all'API e disegna la pagina nel browser.

```
PRIMA (monolite)                      DOPO (separati)
--------------------                  ------------------------------------
Browser ── HTML già pronto ── C#      Browser ── HTML+JS ──fetch(JSON)──> API C#
(Razor costruisce le pagine)          (il JS costruisce la pagina nel browser)
```

Il vantaggio: le due parti si sviluppano, testano e distribuiscono separatamente, e domani
lo stesso backend può servire più frontend (sito web, app mobile…).

### Cosa cambia nel backend (C#)
1. **I controller restituiscono JSON, non viste.** Al posto di `return View(...)` si usa
   `return Ok(dati)`. Il progetto ha già un esempio di questo stile: `StatusController`
   (`[ApiController]`, `return Ok("OK")`). In pratica gli attuali `ShipController` e
   `SchedulerController` diventano controller API con metodi tipo `GET /api/ships`,
   `POST /api/ships`, `GET /api/scheduler/board`, `POST /api/scheduler/assign`.
2. **Si usano i DTO** (Data Transfer Object). Invece di spedire le entity del database così
   come sono, si creano piccole classi che contengono *solo* i campi da mostrare (es. un
   `ShipDto` con `Id, Name, Size, Status`). Serve a non esporre dettagli interni e a evitare
   riferimenti circolari quando si converte in JSON.
3. **Si abilita la CORS.** Il browser, per sicurezza, blocca le chiamate JavaScript verso un
   "indirizzo" (origine) diverso da quello della pagina. Se il frontend gira su una porta e
   l'API su un'altra, bisogna dire all'API di accettarle: in `Program.cs` si aggiunge
   `builder.Services.AddCors(...)` e `app.UseCors(...)` autorizzando l'origine del frontend.
4. **L'autenticazione.** Con file HTML statici serviti da un'altra origine, il modo più comune
   è passare da cookie a **token**: al login l'API restituisce un **JWT** (una stringa firmata);
   il frontend lo salva e lo rimanda a ogni richiesta nell'header `Authorization: Bearer <token>`.
   In alternativa, per restare semplici all'inizio, si può **servire i file statici dallo stesso
   backend** (cartella `wwwroot`): stessa origine → i cookie attuali continuano a funzionare e la
   CORS non serve.
5. **Le viste Razor spariscono** (o restano solo per un'eventuale pagina di cortesia): la parte
   `Views/` non è più responsabilità del backend.

### Come sarebbe il frontend in HTML + JavaScript
Un piccolo insieme di file statici, senza framework:

```
frontend/
  index.html      struttura della pagina (tabelle vuote, form di login…)
  app.js          la logica: chiama l'API e riempie la pagina
  style.css       lo stile (si può riusare quello attuale in wwwroot/css)
```

L'idea del JavaScript: chiedere i dati con `fetch` e costruire l'HTML a partire dalla risposta.

```javascript
// Esempio: caricare l'elenco navi e metterlo in una tabella
async function caricaNavi() {
  const risposta = await fetch("http://localhost:8080/api/ships");
  const navi = await risposta.json();            // array di oggetti nave
  const corpo = document.querySelector("#tabella-navi tbody");
  corpo.innerHTML = "";                          // svuota
  for (const nave of navi) {
    const riga = document.createElement("tr");
    riga.innerHTML = `<td>${nave.name}</td><td>${nave.size}</td><td>${nave.status}</td>`;
    corpo.appendChild(riga);
  }
}
```

Per **provarlo** basta aprire `index.html` con un piccolo server statico (es. l'estensione
*Live Server* di VS Code, oppure la cartella `wwwroot` del backend). Il login funziona chiamando
`POST /api/login` e, a seconda della scelta al punto 4, salvando il token o affidandosi al cookie.

### In sintesi (passaggio a frontend separato)
1. Trasformare i controller in API JSON (+ DTO). 2. Abilitare CORS *oppure* servire il frontend da
`wwwroot`. 3. Decidere l'autenticazione (cookie stessa-origine o JWT). 4. Scrivere `index.html` +
`app.js` che consumano l'API con `fetch`.

---

## Passo successivo: passare a React

Quando il frontend "a mano" in HTML+JS inizia a diventare grande, aggiornare la pagina con
`document.createElement` e `innerHTML` diventa faticoso e pieno di errori. **React** è una libreria
JavaScript che risolve proprio questo: descrivi *com'è fatta* la pagina in base ai dati, e ci pensa
lui a ridisegnarla quando i dati cambiano.

> Prerequisito: il backend è già l'**API JSON** della sezione precedente. React sostituisce solo il
> frontend HTML+JS; il backend C# **non cambia**.

### Cosa serve installare
- **Node.js** (include `npm`, il gestore dei pacchetti JavaScript). Serve *solo per sviluppare* il
  frontend; il risultato finale sono comunque file statici.

### Creare il progetto
Si usa uno strumento che prepara tutto (qui **Vite**, molto comune e veloce):

```bash
npm create vite@latest frontend -- --template react
cd frontend
npm install
npm run dev          # avvia il frontend di sviluppo, di solito su http://localhost:5173
```

### Le idee nuove rispetto all'HTML+JS
- **Componenti.** La pagina si spezza in pezzi riutilizzabili (es. `ListaNavi`, `RigaNave`,
  `FormLogin`). Ogni componente è una funzione che restituisce del "HTML in JavaScript" (**JSX**).
- **Stato (`useState`).** Ogni componente può avere dei dati che, quando cambiano, fanno
  ri-disegnare *automaticamente* la parte di pagina interessata: niente più `innerHTML` a mano.
- **Effetti (`useEffect`).** Il posto dove fare le chiamate all'API (es. caricare le navi appena il
  componente appare sullo schermo).

```jsx
// Stesso esempio di prima, ma in React
import { useEffect, useState } from "react";

function ListaNavi() {
  const [navi, setNavi] = useState([]);                 // lo "stato": le navi

  useEffect(() => {                                     // al primo render, carica dall'API
    fetch("http://localhost:8080/api/ships")
      .then(r => r.json())
      .then(setNavi);
  }, []);

  return (                                              // React ridisegna quando "navi" cambia
    <table>
      <tbody>
        {navi.map(n => (
          <tr key={n.id}><td>{n.name}</td><td>{n.size}</td><td>{n.status}</td></tr>
        ))}
      </tbody>
    </table>
  );
}
```

### Come organizzare il progetto React
```
frontend/
  src/
    components/     i componenti (ListaNavi.jsx, FormLogin.jsx, Board.jsx…)
    services/       le chiamate all'API raccolte in un punto solo (api.js)
    App.jsx         mette insieme i componenti e gestisce le pagine
```
- **`services/api.js`**: si mettono qui tutte le `fetch`, così i componenti restano puliti.
- **Navigazione tra pagine** (login, elenco navi, board): si aggiunge la libreria
  **React Router** (`npm install react-router-dom`).
- **Indirizzo dell'API**: non scriverlo fisso nel codice, ma in una **variabile d'ambiente**
  (con Vite, un file `.env` con `VITE_API_URL=...`), così è facile cambiarlo tra sviluppo e
  produzione.

### CORS e "proxy" in sviluppo
In sviluppo il frontend gira su una porta (5173) e l'API su un'altra (8080): torna il tema CORS.
Due modi:
- lasciare la **CORS** abilitata sul backend (come nella sezione precedente); **oppure**
- configurare il **proxy di Vite**: si dice a Vite di inoltrare le richieste che iniziano per
  `/api` verso `http://localhost:8080`. Così, dal punto di vista del browser, frontend e API
  sembrano la stessa origine e la CORS non serve durante lo sviluppo.

### Andare in produzione
`npm run build` genera una cartella `dist/` di **file statici** (HTML, JS, CSS "compattati").
Da lì due strade tipiche: servirli con un server statico dedicato (es. nginx), oppure copiarli
dentro `wwwroot` del backend C# e farli servire dallo stesso ASP.NET Core (frontend e API sulla
stessa origine → niente CORS anche in produzione).

### In sintesi (passaggio a React)
1. Installare Node.js e creare il progetto con Vite. 2. Costruire l'interfaccia con **componenti** +
**stato** invece di manipolare l'HTML a mano. 3. Raccogliere le chiamate API in `services/` e usare
React Router per le pagine. 4. Gestire CORS/proxy in sviluppo. 5. `npm run build` e distribuire i file
statici (server dedicato o `wwwroot` del backend). **Il backend C# resta lo stesso.**
</content>
</invoke>
