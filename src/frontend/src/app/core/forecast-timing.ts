/** True when the first target calendar day had already begun in Sri Lanka at issue time. */
export function firstForecastDayAlreadyStarted(createdAt: string, firstTargetDate: string): boolean {
  const issuedAt = new Date(createdAt);
  if (Number.isNaN(issuedAt.getTime()) || !/^\d{4}-\d{2}-\d{2}$/.test(firstTargetDate)) return false;
  const parts = new Intl.DateTimeFormat('en', {
    timeZone: 'Asia/Colombo', year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(issuedAt);
  const value = (type: string) => parts.find(part => part.type === type)?.value ?? '';
  return `${value('year')}-${value('month')}-${value('day')}` >= firstTargetDate;
}
