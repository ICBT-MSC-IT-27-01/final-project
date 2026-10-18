import { describe, expect, it } from 'vitest';
import { firstForecastDayAlreadyStarted } from './forecast-timing';

describe('firstForecastDayAlreadyStarted', () => {
  it('flags a run issued after its first Sri Lankan target day began', () => {
    expect(firstForecastDayAlreadyStarted('2026-10-17T15:38:02Z', '2026-10-17')).toBe(true);
  });

  it('accepts a run issued before the first Sri Lankan target day', () => {
    expect(firstForecastDayAlreadyStarted('2026-10-16T15:38:02Z', '2026-10-17')).toBe(false);
  });
});
