export function EmptyState({ title, body, testId }: { title: string; body: string; testId: string }) {
  return (
    <div className="empty" data-testid={testId}>
      <h2>{title}</h2>
      <p>{body}</p>
    </div>
  );
}
