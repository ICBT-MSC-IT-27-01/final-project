# Phase 16 UAT Test Plan

Generated: 2026-10-13

## UAT Status

Real participant UAT: NOT PERFORMED.

This document prepares UAT instruments only. Actual result, rating, and comment fields must remain blank until real participants provide responses under approved ethics conditions.

## Test Case Template

| Field | Value |
| --- | --- |
| Scenario ID |  |
| User role |  |
| Preconditions |  |
| Action steps |  |
| Expected result |  |
| Actual result |  |
| Pass/fail |  |
| Usability rating |  |
| Comments |  |
| Issue severity |  |
| Evidence reference |  |

## Public User Scenarios

| Scenario ID | User role | Preconditions | Action steps | Expected result |
| --- | --- | --- | --- | --- |
| UAT-PUB-01 | Public User | Application available | Open dashboard | Latest forecast or no-data state is understandable |
| UAT-PUB-02 | Public User | Forecast exists | Open recommendations and submit without soil | Six crop results or explicit insufficient-evidence states appear |
| UAT-PUB-03 | Public User | API failure simulated | Submit recommendation | Safe error message is displayed |

## Registered User Scenarios

| Scenario ID | User role | Preconditions | Action steps | Expected result |
| --- | --- | --- | --- | --- |
| UAT-REG-01 | Registered User | No account | Register | Account is created as Registered User only |
| UAT-REG-02 | Registered User | Valid account | Login then logout | Session starts and ends clearly |
| UAT-REG-03 | Registered User | History enabled/test fixture exists | Open history | Saved recommendations display without exposing other users |

## Agricultural Officer Scenarios

| Scenario ID | User role | Preconditions | Action steps | Expected result |
| --- | --- | --- | --- | --- |
| UAT-OFF-01 | Agricultural Officer | Officer account exists | Open pending validations | Pending recommendations are listed |
| UAT-OFF-02 | Agricultural Officer | Recommendation exists | Submit validation status/comment | Validation record is accepted |
| UAT-OFF-03 | Agricultural Officer | Officer logged in | Try admin navigation | Admin controls are unavailable |

## Administrator Scenarios

| Scenario ID | User role | Preconditions | Action steps | Expected result |
| --- | --- | --- | --- | --- |
| UAT-ADM-01 | Administrator | Admin account exists | Open admin dashboard | Admin sections are visible |
| UAT-ADM-02 | Administrator | Feature disabled/test mode | Trigger operational forecast | Safe disabled/error state appears |
| UAT-ADM-03 | Administrator | User management available | View users | Role management is available only to Administrator |

## Participant Feedback Fields

Collect only the minimum needed:

- Role category
- Scenario ID
- Pass/fail
- Usability rating
- Free-text comments
- Optional issue severity

Do not collect unnecessary personal data.
