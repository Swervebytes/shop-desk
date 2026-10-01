export type Client = {
  id: string;
  name: string;
  contact: string;
};

export type Job = {
  id: string;
  clientId: string;
  clientName: string;
  title: string;
  status: string;
  dueDate: string;
  notes: string;
};

export const STATUSES = [
  { value: "new", label: "New" },
  { value: "in_progress", label: "In progress" },
  { value: "review", label: "Review" },
  { value: "done", label: "Done" }
] as const;

export function statusLabel(status: string) {
  return STATUSES.find((item) => item.value === status)?.label ?? status;
}

export function formatDue(iso: string) {
  const [year, month, day] = iso.split("-").map(Number);
  if (!year || !month || !day) return iso;
  return new Date(year, month - 1, day).toLocaleDateString(undefined, {
    month: "short",
    day: "numeric",
    year: "numeric"
  });
}

export function isPastDue(iso: string, status: string) {
  if (status === "done") return false;
  const [year, month, day] = iso.split("-").map(Number);
  if (!year || !month || !day) return false;
  const due = new Date(year, month - 1, day);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return due < today;
}

export function isoInDays(days: number) {
  const date = new Date();
  date.setDate(date.getDate() + days);
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}
