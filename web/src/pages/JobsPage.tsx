import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { EmptyState } from "../components/EmptyState";
import { StatusPill } from "../components/StatusPill";
import { formatDue, isPastDue, STATUSES, type Job } from "../types";

export function JobsPage() {
  const [draft, setDraft] = useState("");
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("");
  const [jobs, setJobs] = useState<Job[] | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    const params = new URLSearchParams();
    if (status) params.set("status", status);
    if (query) params.set("q", query);
    const suffix = params.size ? `?${params.toString()}` : "";
    let cancelled = false;
    api<Job[]>(`/api/jobs${suffix}`)
      .then((rows) => {
        if (!cancelled) {
          setJobs(rows);
          setError("");
        }
      })
      .catch((caught) => {
        if (!cancelled) setError(caught instanceof Error ? caught.message : "Could not load jobs.");
      });
    return () => {
      cancelled = true;
    };
  }, [query, status]);

  function onSearch(event: FormEvent) {
    event.preventDefault();
    setQuery(draft.trim());
  }

  const filtering = query.length > 0 || status.length > 0;

  return (
    <section className="page">
      <div className="page-head">
        <div>
          <p className="eyebrow">Board</p>
          <h1>Jobs</h1>
        </div>
        <Link className="button" to="/jobs/new" data-testid="add-job">Add job</Link>
      </div>

      <form className="filters" onSubmit={onSearch}>
        <label className="grow">
          Search
          <input
            data-testid="job-search"
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            placeholder="Title, note, or client"
          />
        </label>
        <button type="submit" className="secondary" data-testid="job-search-submit">Search</button>
        <div className="choices" role="group" aria-label="Status filter">
          <button
            type="button"
            className={status === "" ? "choice on" : "choice"}
            data-testid="filter-all"
            onClick={() => setStatus("")}
          >
            All
          </button>
          {STATUSES.map((item) => (
            <button
              type="button"
              key={item.value}
              className={status === item.value ? "choice on" : "choice"}
              data-testid={`filter-${item.value}`}
              onClick={() => setStatus(item.value)}
            >
              {item.label}
            </button>
          ))}
        </div>
      </form>

      {error ? <p className="form-error" role="alert">{error}</p> : null}

      {jobs === null ? <p className="muted">Loading jobs…</p> : null}

      {jobs && jobs.length === 0 ? (
        <EmptyState
          testId="empty-jobs"
          title={filtering ? "No jobs match" : "No jobs yet"}
          body={filtering
            ? "Try another status, or search for a different title, note, or client."
            : "Add a job when a client has work for the shop."}
        />
      ) : null}

      {jobs && jobs.length > 0 ? (
        <ul className="job-list">
          {jobs.map((job) => (
            <li key={job.id}>
              <Link className="job-row" to={`/jobs/${job.id}`} data-testid="job-link">
                <span className="job-title">{job.title}</span>
                <span className="job-client">{job.clientName}</span>
                <StatusPill status={job.status} />
                <span className={isPastDue(job.dueDate, job.status) ? "due late" : "due"}>
                  {formatDue(job.dueDate)}
                  {isPastDue(job.dueDate, job.status) ? " · Past due" : ""}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
