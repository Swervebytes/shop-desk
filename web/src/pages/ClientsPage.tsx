import { useEffect, useState, type FormEvent } from "react";
import { api } from "../api";
import { EmptyState } from "../components/EmptyState";
import type { Client } from "../types";

export function ClientsPage() {
  const [clients, setClients] = useState<Client[] | null>(null);
  const [name, setName] = useState("");
  const [contact, setContact] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);

  function load() {
    return api<Client[]>("/api/clients")
      .then((rows) => {
        setClients(rows);
        setError("");
      })
      .catch((caught) => setError(caught instanceof Error ? caught.message : "Could not load clients."));
  }

  useEffect(() => {
    void load();
  }, []);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setPending(true);
    setError("");
    try {
      await api<Client>("/api/clients", {
        method: "POST",
        body: JSON.stringify({ name, contact })
      });
      setName("");
      setContact("");
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not add the client.");
    } finally {
      setPending(false);
    }
  }

  return (
    <section className="page">
      <div className="page-head">
        <div>
          <p className="eyebrow">People</p>
          <h1>Clients</h1>
        </div>
      </div>
      <div className="split">
        <form className="panel stack" onSubmit={onSubmit}>
          <h2>Add a client</h2>
          <label>
            Name
            <input
              data-testid="client-name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              required
              maxLength={200}
            />
          </label>
          <label>
            Contact
            <input
              data-testid="client-contact"
              value={contact}
              onChange={(event) => setContact(event.target.value)}
              required
              maxLength={200}
              placeholder="Email or phone"
            />
          </label>
          {error ? <p className="form-error" role="alert">{error}</p> : null}
          <button type="submit" data-testid="client-save" disabled={pending}>
            {pending ? "Adding…" : "Add client"}
          </button>
        </form>
        <div>
          {clients === null && !error ? <p className="muted">Loading clients…</p> : null}
          {clients && clients.length === 0 ? (
            <EmptyState
              testId="empty-clients"
              title="No clients yet"
              body="Add a client before you open a job for them."
            />
          ) : null}
          {clients && clients.length > 0 ? (
            <ul className="client-list">
              {clients.map((client) => (
                <li key={client.id} className="client-row" data-testid="client-row">
                  <strong>{client.name}</strong>
                  <span>{client.contact}</span>
                </li>
              ))}
            </ul>
          ) : null}
        </div>
      </div>
    </section>
  );
}
