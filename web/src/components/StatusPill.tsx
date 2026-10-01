import { statusLabel } from "../types";

export function StatusPill({ status }: { status: string }) {
  return (
    <span className={`pill pill-${status}`} data-testid="job-status">
      {statusLabel(status)}
    </span>
  );
}
