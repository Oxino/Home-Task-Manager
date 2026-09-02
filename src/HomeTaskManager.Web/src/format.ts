// Shared time/duration formatting: always shown as hours and minutes (e.g. "14h12"), never decimal hours.
export function formatDurationFromMinutes(totalMinutes: number): string {
  const rounded = Math.max(0, Math.round(totalMinutes));
  const hours = Math.floor(rounded / 60);
  const minutes = rounded % 60;
  if (hours === 0) return `${minutes}min`;
  return `${hours}h${String(minutes).padStart(2, "0")}`;
}

export function formatDurationFromHours(hours: number): string {
  return formatDurationFromMinutes(hours * 60);
}
