# BlueHarbor — Registro Operativo Terminal

Applicazione web interna per il coordinamento di un piccolo terminal container fittizio.
Progetto *ITS Learning by Project 2025–2027*. Tutti i dati e le regole sono didattici.

## Come avviare

Con il .NET 8 SDK installato:

```bash
dotnet run
```

Senza installare nulla (solo Docker) → vedi **[DOCKER.md](DOCKER.md)**:

```bash
docker compose up --build
```

Applicazione su <http://localhost:8080> (redirige al login).

### Utenti demo
| Utente | Password | Ruolo |
|-----------|-----------|------------|
| `operatore` | `operatore` | Operatore |
| `scheduler` | `scheduler` | Scheduler |

### Link utili
- App: <http://localhost:8080>
- Swagger UI (endpoint tecnico di status): <http://localhost:8080/swagger>

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

## Fuori scope (come da traccia)
Nessuna pianificazione/ottimizzazione automatica, nessun KPI, nessun real-time, nessuna
modifica o riassegnazione dopo l'assegnazione.

## Stack tecnico
.NET 8 · ASP.NET Core MVC · Entity Framework Core (InMemory) · Cookie Authentication ·
Razor · Swashbuckle (Swagger).
