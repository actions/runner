# YAML getting started - Pipeline overview

## The plan

When a pipeline is started, the execution plan is created first.

The plan contains stages. Stages contain jobs. Jobs contain steps.

```
---------------------------------------------
|                   Plan                    |
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

Stages provide a logical boundary within the plan.

The stage boundary allows:
- Manual checkpoints or approvals between stages
- Reporting on high level results (email notifications, build badges)

## Jobs

Jobs are a grouping of steps, and are assigned to a specific target.

For example, when a job targets an agent pool, the job will be assigned to one of the agents running within the pool.

## Steps

Steps are the individual units of execution within a job. For example, run a script.
