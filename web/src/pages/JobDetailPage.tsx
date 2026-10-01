import { useEffect, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api";
import { EmptyState } from "../components/EmptyState";
import { formatDue, STATUSES, type Job } from "../types";

export function JobDetailPage() {
  const { id } = useParams();
  const [job, setJob] = useState<Job | null>(null);
  const [status, setStatus] = useState("new");
  const [notes, setNotes] = useState("");
  const [missing, setMissing] = useState(false);
  const [error, setError] = useState("");
  const [saved, setSaved] = useState("");
  const [pending, setPending] = useState(false);

  useEffect(() => {
    if (!id) return;
    api<Job>(`/api/jobs/${id}`)
      .then((row) => {
        setJob(row);
        setStatus(row.status);
        setNotes(row.notes);
      })
      .catch((caught) => {
        if (caught instanceof Error && caught.message.toLowerCase().includes("not found")) {
          setMissing(true);
          return;
        }
        setError(caught instanceof Error ? caught.message : "Could not load the job.");
      });
  }, [id]);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!job) return;
    setPending(true);
    setSaved("");
    setError("");
    try {
      const updated = await api<Job>(`/api/jobs/${job.id}`, {
        method: "PATCH",
        body: JSON.stringify({ status, notes })
      });
      setJob(updated);
      setStatus(updated.status);
      setNotes(updated.notes);
      setSaved("Saved.");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not save the job.");
    } finally {
      setPending(false);
    }
  }

  if (missing) {
    return (
      <section className="page narrow">
        <EmptyState testId="empty-job" title="Job not found" body="That job is not on the board." />
        <p><Link to="/jobs">Back to jobs</Link></p>
      </section>
    );
  }

  if (!job) {
    return (
      <section className="page narrow">
        {error ? <p className="form-error" role="alert">{error}</p> : <p className="muted">Loading job…</p>}
      </section>
    );
  }

  return (
    <section className="page narrow">
      <p className="eyebrow">{job.clientName}</p>
      <h1 data-testid="job-detail-title">{job.title}</h1>
      <p className="lede">Due {formatDue(job.dueDate)} · <Link to="/jobs">Back to jobs</Link></p>

      <form className="stack" onSubmit={onSubmit}>
        <fieldset>
          <legend>Status</legend>
          <div className="choices" role="radiogroup" aria-label="Status">
            {STATUSES.map((item) => (
              <button
                type="button"
                key={item.value}
                role="radio"
                aria-checked={status === item.value}
                className={status === item.value ? "choice on" : "choice"}
                data-testid={`status-${item.value}`}
                onClick={() => setStatus(item.value)}
              >
                {item.label}
              </button>
            ))}
          </div>
        </fieldset>
        <label>
          Note
          <textarea
            data-testid="note-input"
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            rows={5}
            maxLength={4000}
            placeholder="Add a note"
          />
        </label>
        {error ? <p className="form-error" role="alert">{error}</p> : null}
        {saved ? <p className="form-ok" role="status" data-testid="save-ok">{saved}</p> : null}
        <button type="submit" data-testid="save-job" disabled={pending}>
          {pending ? "Saving…" : "Save changes"}
        </button>
      </form>
    </section>
  );
}
