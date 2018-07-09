# YAML getting started - Pipeline overview

## Pipeline

A pipeline contains phases. Phases contain steps.

```
-----------------------------------
|            Pipeline             |
|                                 |
|    -------------------------    |
|    |        Phases         |    |
|    |                       |    |
|    |    ---------------    |    |
|    |    |    Steps    |    |    |
|    |    ---------------    |    |
|    |                       |    |
|    -------------------------    |
|                                 |
-----------------------------------
```

<!-- A pipeline contains stages. Stages contain jobs. Jobs contain steps.

```
---------------------------------------------
|                 Pipeline                  |
|                                           |
|    -----------------------------------    |
|    |             Stages              |    |
|    |                                 |    |
|    |    -------------------------    |    |
|    |    |         Jobs          |    |    |
|    |    |                       |    |    |
|    |    |    ---------------    |    |    |
|    |    |    |    Steps    |    |    |    |
|    |    |    ---------------    |    |    |
|    |    |                       |    |    |
|    |    -------------------------    |    |
|    |                                 |    |
|    -----------------------------------    |
|                                           |
---------------------------------------------
```

## Stages

Stages provide a logical boundary within the pipeline.

The stage boundary allows:
- Manual checkpoints or approvals between stages
- Reporting on high level results (email notifications, build badges) -->

## Phases

A phase is a group of steps, and is assigned to a specific target.

For example, when a phase targets an agent pool, the phase will be assigned to one of the agents running within the pool.

## Steps

Steps are the individual units of execution within a phase. For example, run a script.
