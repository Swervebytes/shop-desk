import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api } from "../api";
import { EmptyState } from "../components/EmptyState";
import { isoInDays, type Client, type Job } from "../types";

export function JobFormPage() {
  const navigate = useNavigate();
  const [clients, setClients] = useState<Client[] | null>(null);
  const [clientId, setClientId] = useState("");
  const [title, setTitle] = useState("");
  const [dueDate, setDueDate] = useState(isoInDays(7));
  const [notes, setNotes] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);

  useEffect(() => {
    api<Client[]>("/api/clients")
      .then((rows) => {
        setClients(rows);
        if (rows[0]) setClientId(rows[0].id);
      })
      .catch((caught) => setError(caught instanceof Error ? caught.message : "Could not load clients."));
  }, []);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError("");
    setPending(true);
    try {
      const job = await api<Job>("/api/jobs", {
        method: "POST",
        body: JSON.stringify({
          clientId,
          title,
          dueDate,
          notes
        })
      });
      navigate(`/jobs/${job.id}`);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not create the job.");
      setPending(false);
    }
  }

  return (
    <section className="page narrow">
      <p className="eyebrow">New work</p>
      <h1>Add a job</h1>
      <p className="lede"><Link to="/jobs">Back to jobs</Link></p>

      {clients && clients.length === 0 ? (
        <EmptyState
          testId="empty-clients-for-job"
          title="No clients yet"
          body="Add a client before you open a job for them."
        />
      ) : null}

      {clients && clients.length === 0 ? <p><Link className="button" to="/clients">Add a client</Link></p> : null}

      {clients && clients.length > 0 ? (
        <form className="stack" onSubmit={onSubmit}>
          <label>
            Title
            <input
              data-testid="job-title"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              required
              maxLength={200}
            />
          </label>
          <label>
            Client
            <select
              data-testid="job-client"
              value={clientId}
              onChange={(event) => setClientId(event.target.value)}
              required
            >
              {clients.map((client) => (
                <option key={client.id} value={client.id}>{client.name}</option>
              ))}
            </select>
          </label>
          <label>
            Due date
            <input
              data-testid="job-due"
              type="date"
              value={dueDate}
              onChange={(event) => setDueDate(event.target.value)}
              required
            />
          </label>
          <label>
            Note
            <textarea
              data-testid="job-notes"
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
              rows={4}
              maxLength={4000}
              placeholder="What does the shop need to do?"
            />
          </label>
          {error ? <p className="form-error" role="alert">{error}</p> : null}
          <button type="submit" data-testid="job-save" disabled={pending}>
            {pending ? "Creating…" : "Create job"}
          </button>
        </form>
      ) : null}

      {clients === null && !error ? <p className="muted">Loading clients…</p> : null}
      {error && clients === null ? <p className="form-error" role="alert">{error}</p> : null}
    </section>
  );
}
