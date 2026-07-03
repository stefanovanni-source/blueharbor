# Avviare BlueHarbor con Docker

Questa guida spiega come compilare ed eseguire l'applicazione **senza installare
il .NET SDK** (né altri strumenti) sul tuo computer. Serve **solo Docker**: la
compilazione avviene dentro il container, usando l'SDK contenuto nell'immagine.

## Prerequisito unico

Installa **Docker Desktop** (Windows/macOS) oppure **Docker Engine** (Linux):
<https://www.docker.com/products/docker-desktop/>

Verifica che sia attivo:

```bash
docker --version
```

---

## Metodo 1 — Docker Compose (consigliato)

Dalla cartella del progetto (quella che contiene il `Dockerfile`):

```bash
docker compose up --build
```

- La prima esecuzione scarica le immagini .NET e compila l'app (qualche minuto).
- Le esecuzioni successive sono molto più rapide grazie alla cache.

Apri il browser su: <http://localhost:8080>

Per fermare l'applicazione: premi `Ctrl+C`, poi (per rimuovere il container):

```bash
docker compose down
```

Per avviarla in background (senza tenere occupato il terminale):

```bash
docker compose up --build -d      # avvia in background
docker compose logs -f            # segue i log
docker compose down               # ferma e rimuove
```

---

## Metodo 2 — Docker "puro" (senza Compose)

Compila l'immagine:

```bash
docker build -t blueharbor .
```

Avvia il container mappando la porta 8080:

```bash
docker run --rm -p 8080:8080 blueharbor
```

Apri: <http://localhost:8080> — per fermare premi `Ctrl+C`.

---

## Uso dell'applicazione

Dopo l'avvio, l'app redirige alla pagina di login.

| Utente | Password | Ruolo |
|-------------|-------------|-------------|
| `operatore` | `operatore` | Operatore (registra e gestisce le navi) |
| `scheduler` | `scheduler` | Scheduler (assegna le navi alle banchine) |

Link utili:
- App: <http://localhost:8080>
- Endpoint tecnico di status (Swagger): <http://localhost:8080/swagger>

---

## Note

- **Dati non persistenti**: il database è in-memory. Ogni volta che il container
  si ferma, i dati si azzerano e all'avvio successivo vengono ricreati orologio
  (giorno 1) e banchine fisse (1 XL, 1 L, 2 M, 4 S). È il comportamento previsto
  dall'esercizio.
- **Porta già occupata**: se la 8080 è usata da un altro servizio, cambia la
  mappatura, per esempio `-p 9090:8080` (poi apri <http://localhost:9090>), oppure
  modifica `ports` in `docker-compose.yml` (`"9090:8080"`).
- **Ricompilare dopo modifiche al codice**: riesegui con `--build`
  (`docker compose up --build`) per rigenerare l'immagine.

---

## Come funziona il Dockerfile

Il `Dockerfile` usa una **build multi-stage**:

1. **Stage `build`** (`mcr.microsoft.com/dotnet/sdk:8.0`): contiene l'SDK .NET,
   ripristina i pacchetti NuGet e pubblica l'app in `Release`.
2. **Stage `runtime`** (`mcr.microsoft.com/dotnet/aspnet:8.0`): immagine leggera
   con il **solo runtime**; ci copia dentro l'output pubblicato e avvia
   `BlueHarbor.dll`.

Risultato: l'SDK resta confinato nella fase di build e **non è richiesto nulla
sulla macchina host oltre a Docker**.
