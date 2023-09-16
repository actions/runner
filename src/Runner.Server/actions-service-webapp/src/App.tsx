import React, { useEffect, useMemo, useState } from 'react';
import './App.css';
import { Link, Navigate, NavLink, Route, Routes, useParams, Params, useResolvedPath, resolvePath, LinkProps } from 'react-router-dom';
import Convert from 'ansi-to-html'; 
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import remarkBreaks from 'remark-breaks'
import rehypeRaw from 'rehype-raw';
import rehypeHighlight from 'rehype-highlight';
import { CircleIcon, SkipIcon, StopIcon, XCircleFillIcon, CheckCircleFillIcon, ChevronDownIcon, ChevronRightIcon, GitCommitIcon, RepoIcon, PersonIcon, MeterIcon } from '@primer/octicons-react'
import { ghHostApiUrl } from './config';

var convert = new Convert({
  newline: true,
  escapeXML: true
});

function List({ fullscreen } : { fullscreen?: boolean }) {
  var params = useParams();
  const [ jobs, setJobs ] = useState<IJob[]>([]);
  var page = Number.parseInt(params["page"] || "0");
  var parentPath = useResolvedPath("..");
  var resolved = resolvePath("..", parentPath.pathname);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>("");
  useEffect(() => {
    setJobs([]);
    if(params.page) {
        var runids = params.runid ? `&runid=${encodeURIComponent(params.runid || "")}` : "";
        (async () => {
        try {
          setLoading(true);
          var resp = await fetch(`${ghHostApiUrl}/_apis/v1/Message?page=${encodeURIComponent(page)}&repo=${encodeURIComponent(params.owner && params.repo ? (params.owner + "/" + params.repo) : "")}${runids}`, { })
          if(resp.status === 200) {
            var jobs : IJob[] | null = await resp.json();
            setJobs(jobs || []);
          }
          setError("");
        } catch(ex) {
          if(ex instanceof Object) {
            setError(ex.toString());
          } else {
            setError("Unknown Error: " + ex);
          }
        } finally {
          setLoading(false);
        }
      })()
      var source = new EventSource(`${ghHostApiUrl}/_apis/v1/Message/event2?owner=${encodeURIComponent(params.owner || "")}&repo=${encodeURIComponent(params.repo || "")}${runids}`);
      if(page === 0) {
        source.addEventListener("job", ev => {
          var x = JSON.parse((ev as MessageEvent).data) as IJob;
          setJobs(_jobs => {
              for(var i in _jobs) {
                if(_jobs[i].jobId === x.jobId) {
                  var f = [..._jobs];
                  f[i] = x;
                  return f;
                }
              }
              var jobs = [..._jobs];
              var insertp = jobs.findIndex(j => j.runid > x.runid && j.attempt > x.attempt && j.requestId > x.requestId);
              var sp = insertp > 0 ? jobs.splice(insertp) : jobs.splice(0);
              if(sp.length > 0 && sp[0].jobId === x.jobId) {
                sp.shift();
              }
              var final = [...jobs, x, ...sp];
              // Remove elements from the first page
              if(final.length > 30) {
                  final.length = 30;
              }
              return final;
          });
        });
      }
      source.addEventListener("jobupdate", ev => {
        var x = JSON.parse((ev as MessageEvent).data) as IJob;
        setJobs(_jobs => {
          for(var i in _jobs) {
            if(_jobs[i].jobId === x.jobId) {
              var final = [..._jobs];
              final[i] = x;
              return final;
            }
          }
          return _jobs;
        });
      });
      return () => source.close();
    }
  }, [page, params.page, params.owner, params.repo, params.runid]);
  return (<span style={fullscreen ? {width: '100%', height: '100%'} : {
      maxWidth: '400px',
      width: '40vw',
      height: '100%',
      overflowY: 'auto',
      border: '0',
      borderRight: '1px',
      borderColor: 'gray',
      borderStyle: 'solid'}}>
    <Link className='btn btn-outline-secondary w-100' to={resolved}>Back</Link>
    {params.runid ? (<Link className='btn btn-outline-primary w-100' to={"."}>Summary</Link>) : (<></>)}
    <div className="btn-group w-100" role="group">
      <DisableableLink className='btn btn-secondary w-50' disabled={page <= 0} to={"../"+ (page - 1) + (fullscreen ? "" : "/" + params['*'])}>Previous</DisableableLink>
      <Link className='btn btn-primary w-50' to={"../"+ (page + 1) + (fullscreen ? "" : "/" + params['*'])}>Next</Link>
    </div>
    {jobs.map(val => (
      <NavLink key={val.jobId} to={encodeURIComponent(val.jobId)} className={({isActive})=> isActive ? 'btn btn-outline-secondary w-100 text-start active' : 'btn btn-outline-secondary w-100 text-start'}><span style={{fontSize: 20}}>{val.name}</span><br/><span style={{fontSize: 12}}>{!params.runid ? (<>repo:&nbsp;{val.repo} workflow:&nbsp;{val.workflowname} runid:&nbsp;{val.runid} </>) : (<></>)}attempt:&nbsp;{val.attempt} result:&nbsp;<TimelineStatus status={val.result ?? "inprogress"}/></span></NavLink>
    ))}
  { loading ? <span>Loading...</span> : error ? <span>{error}</span> : <></> }
  </span>);
};

interface GenericListProps<T> {
  url: (params : Readonly<Params<string>>) => string
  id: (el : T) => string
  summary: (el : T, params : Readonly<Params<string>>) => JSX.Element
  hasBack?: boolean
  externalBackUrl?: (params : Readonly<Params<string>>) => string | undefined
  externalBackLabel?: (params : Readonly<Params<string>>) => string
  actions?: (el : T, params : Readonly<Params<string>>) => JSX.Element
  eventName?: string
  eventUpdateName?: string
  eventQuery?: (params : Readonly<Params<string>>) => string
}

interface DisableableProp {
  disabled?: boolean
}

const DisableableLink = (param : LinkProps & React.RefAttributes<HTMLAnchorElement> & DisableableProp) => {
  const { disabled, ...otherProps } = param;
  return disabled ? (<button className={param.className} disabled={disabled} >{param.children}</button>) : (<Link {...otherProps}></Link>);
};

const GenericList = <T, >(param : GenericListProps<T>) => {
  var parentPath = useResolvedPath("..");
  var resolved = resolvePath("..", parentPath.pathname);
  var params = useParams();
  const [ jobs, setJobs ] = useState<T[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>("");
  useEffect(() => {
    setJobs([]);
    if(params.page) {
      (async () => {
        try {
          setLoading(true);
          var resp = await fetch(param.url(params), { })
          if(resp.status === 200) {
            var jobs : T[] | null = await resp.json();
            setJobs(jobs || []);
          }
          setError("");
        } catch(ex) {
          if(ex instanceof Object) {
            setError(ex.toString());
          } else {
            setError("Unknown Error: " + ex);
          }
        } finally {
          setLoading(false);
        }
      })()
      var pollNewJobs = (!params.page || params.page === "0") && param.eventName;
      if(pollNewJobs || param.eventUpdateName) {
        var source = new EventSource(`${ghHostApiUrl}/_apis/v1/Message/event2?${(param.eventQuery && param.eventQuery(params)) ?? ""}`);
        if(pollNewJobs && param.eventName) {
          source.addEventListener(param.eventName, ev => {
            var je = JSON.parse((ev as MessageEvent).data) as T;
            setJobs(_jobs => {
                var final = [je, ..._jobs];
                // Remove elements from the first page
                if(final.length > 30) {
                    final.length = 30;
                }
                return final;
            });
          });
        }
        if(param.eventUpdateName) {
          source.addEventListener(param.eventUpdateName, ev => {
            var je = JSON.parse((ev as MessageEvent).data) as T;
            setJobs(_jobs => {
              for(var i in _jobs) {
                if(param.id(_jobs[i]) === param.id(je)) {
                  var final = [..._jobs];
                  final[i] = je;
                  return final;
                }
              }
              return _jobs;
            });
          });
        }
        return () => source.close();
      }
    }
  }, [params, param]);
  var page = Number.parseInt(params["page"] || "0");
  return (<div style={{width: "100%", height: "100%", overflowY: 'auto'}}>
    {param.hasBack || (param.externalBackUrl && param.externalBackLabel && param.externalBackLabel(params)) ?
    <div className="btn-group w-100" role="group">
      {param.hasBack ? <DisableableLink className='btn btn-outline-secondary w-50' to={resolved}>Back</DisableableLink> : <></>}
      {param.externalBackUrl && param.externalBackLabel && param.externalBackLabel(params) ? <a className='btn btn-outline-secondary w-50' href={param.externalBackUrl(params) || ""} target="_blank" rel="noreferrer">{param.externalBackLabel(params)}</a>:<></>}
    </div>
    : <></>}
    <div className="btn-group w-100" role="group">
      <DisableableLink className='btn btn-secondary w-50' disabled={page <= 0} to={"../"+ (page - 1)}>Previous</DisableableLink>
      <Link className='btn btn-primary w-50' to={"../"+ (page + 1)}>Next</Link>
    </div>
    {jobs.map(val => (
      <div key={param.id(val)} className="btn-group w-100" role="group">
        <NavLink to={`${encodeURIComponent(param.id(val))}/0`} className='btn btn-outline-secondary text-start w-100'>{param.summary(val, params)}</NavLink>
        {(param.actions && param.actions(val, params)) || ""}
      </div>
    ))}
    { loading ? <span>Loading...</span> : error ? <span>{error}</span> : <></> }
  </div>);
};

interface ILog {
  id: number,
  location: string
  content: string
}

interface ITimeLine {
  id?: string | undefined,
  parentId?: string | undefined,
  Type?: string | undefined,
  log?: ILog | undefined,
  order?: number | undefined,
  name?: string | undefined,
  busy?: boolean | undefined,
  failed?: boolean | undefined,
  state?: string | undefined,
  result?: string | undefined
  timelineId?: string
}

interface ILogline {
  line : string,
  lineNumber: number
}

interface IRecord {
  value: string[],
  stepId: string,
  startLine: number
  count: number
}

interface ILoglineEvent {
  recordId: string,
  record: IRecord
}

interface ITimeLineEvent {
  timeline: ITimeLine[],
  timelineId: string
}


// Artifacts

export interface ArtifactResponse {
  containerId: string
  size: number
  signedContent: string
  fileContainerResourceUrl: string
  type: string
  name: string
  url: string

  files: ContainerEntry[] | null
}

export interface CreateArtifactParameters {
  Type: string
  Name: string
  RetentionDays?: number
}

export interface PatchArtifactSize {
  Size: number
}

export interface PatchArtifactSizeSuccessResponse {
  containerId: number
  size: number
  signedContent: string
  type: string
  name: string
  url: string
  uploadUrl: string
}

export interface UploadResults {
  uploadSize: number
  totalSize: number
  failedItems: string[]
}

export interface ListArtifactsResponse {
  count: number
  value: ArtifactResponse[]
}

export interface QueryArtifactResponse {
  count: number
  value: ContainerEntry[]
}

export interface ContainerEntry {
  containerId: number
  scopeIdentifier: string
  path: string
  itemType: string
  status: string
  fileLength?: number
  fileEncoding?: number
  fileType?: number
  dateCreated: string
  dateLastModified: string
  createdBy: string
  lastModifiedBy: string
  itemLocation: string
  contentLocation: string
  fileId?: number
  contentId: string
}

/**
* Gets a list of all artifacts that are in a specific container
*/
async function listArtifacts(runid : number): Promise<ListArtifactsResponse> {
const artifactUrl = ghHostApiUrl + "/_apis/pipelines/workflows/" + runid + "/artifacts"

const response = await fetch(artifactUrl);
const body: string = await response.text()
return JSON.parse(body)
}

/**
 * Fetches a set of container items that describe the contents of an artifact
 * @param artifactName the name of the artifact
 * @param containerUrl the artifact container URL for the run
 */
async function getContainerItems(
  artifactName: string,
  containerUrl: string
): Promise<QueryArtifactResponse> {
  // the itemPath search parameter controls which containers will be returned
  const resourceUrl = new URL(containerUrl)
  resourceUrl.searchParams.append('itemPath', artifactName)

  const response = await fetch(resourceUrl.toString());
  const body: string = await response.text()
  return JSON.parse(body)
}

// End Artifacts

export interface IJobCompletedEvent {
  jobId: string,
  requestId: number,
  result: string
}
export interface IJob {
  jobId: string,
  requestId: number,
  timeLineId: string,
  name: string,
  repo: string,
  workflowname: string,
  runid : number,
  errors: string[],
  result: string,
  attempt: number
}

interface ChunkProps {
  lines: string[]
}

const Chunk : React.FC<ChunkProps> = prop => {
  return <>{(prop.lines).map((line, i) =>(
    <span key={i.toString()} style={{textAlign: 'left', whiteSpace: 'pre-wrap', display: "block", overflow: 'auto', fontFamily: "SFMono-Regular,Consolas,Liberation Mono,Menlo,monospace"}} dangerouslySetInnerHTML={{ __html: line }}/>
  ))}</>
};

const MemoChunk = React.memo(Chunk);

interface ChunksProps {
  chunks: string[][]
}

const Chunks : React.FC<ChunksProps> = prop => {
  return <>{prop.chunks.map((chunk, i) => <MemoChunk key={i} lines={chunk}></MemoChunk>)}
  </>
};

const MemoChunks = React.memo(Chunks);

interface DetailProps {
  timeline: ITimeLine | null
  registerLiveLog: (recordId: string, callback: (line: string[]) => void) => void
  unregisterLiveLog: (recordId: string) => void
  render?: boolean
}

interface Content {
  chunks: string[][]
  content: string[]
}

interface AbortControllerMemo {
  controller?: AbortController
  loading?: boolean
}

const Detail : React.FC<DetailProps> = prop => {
  const [content, setContent] = useState<Content>({chunks: [], content: []});
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>("");
  useEffect(() => {
    setContent({chunks: [], content: []});
    setLoading(false);
  }, [prop.timeline?.id]);
  useEffect(() => {
    if(!prop.timeline) {
      return;
    }
    prop.registerLiveLog(prop.timeline.id || "", lines => {
      setContent(content => {
        if(content.content.length > 500) {
          return {chunks: [...content.chunks, [...content.content]], content: [...lines.map(line => convert.toHtml(line))]};
        }
        return {chunks: [...content.chunks], content: [...content.content, ...lines.map(line => convert.toHtml(line))]};
      });
    });
    return () =>{
      prop.unregisterLiveLog(prop.timeline?.id || "");
    };
  }, [prop.timeline?.id, prop]);
  var abortcontroller = useMemo<AbortControllerMemo>(() => ({}), []);
  useEffect(() => {
    abortcontroller.controller = new AbortController();
    abortcontroller.loading = false;
    return () => abortcontroller.controller?.abort();
  }, [prop.timeline?.id, abortcontroller]);
  useEffect(() => {
    if((prop.render === undefined || prop.render) && (prop.timeline?.log?.id || (prop.timeline?.id && prop.timeline?.timelineId)) && !abortcontroller.loading) {
      abortcontroller.loading = true;
      const controller = abortcontroller.controller;
      const signal = controller?.signal;
      (async () => {
        if(prop.timeline?.log?.id) {
          setLoading(true);
          try {
            var response = await fetch(ghHostApiUrl + "/_apis/v1/Logfiles/" + prop.timeline?.log?.id, { signal });
            if(response.status !== 200 && response.status !== 204) {
                throw new Error(`Unexpected http error: ${response.status}`);
            }
            const log = await response.text();
            var lines = log.split(/\r?\n/);
            var offset = '2021-04-02T15:50:14.6619714Z '.length;
            var re = /^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}.[0-9]{7}Z /;
            var j = 0;
            if(signal?.aborted) {
              throw new Error(`Aborted`);
            }
            var callback : () => void
            var registr = setInterval(callback = () => {
              var i = j;
              var len = Math.min(lines.length - i * 5000, 5000);
              if(len <= 0 || signal?.aborted) {
                clearInterval(registr);
                setLoading(false);
                return;
              }
              setContent(content => ({chunks: [...content.chunks.slice(0, i), lines.slice(i * 5000, i * 5000 + len).map((currentValue, i) => convert.toHtml(re.test(currentValue) ? /* new Date(currentValue.substring(0, offset - 1)).toUTCString() + " " +  */ /* (i + 1).toString().padStart(5) + " " + */ currentValue.substring(offset) : currentValue)), ...content.chunks.slice(i)], content: content.content}));
              j++;
            }, 1000);
            callback();
            signal?.addEventListener("abort", () => {
              clearInterval(registr);
              setLoading(false);
            });
            setError("");
          } catch(ex) {
            if(ex instanceof Object) {
              setError(ex.toString());
            } else {
              setError("Unknown Error: " + ex);
            }
          } finally {
            setLoading(false);
          }
        } else if(prop.timeline?.id && prop.timeline?.timelineId) {
          setLoading(true);
          try {
            var logs = await fetch(ghHostApiUrl + "/_apis/v1/TimeLineWebConsoleLog/" + prop.timeline?.timelineId + "/" + prop.timeline?.id, { signal });
            if(logs.status === 200) {
              var missingLines = await logs.json() as ILogline[];
              if(signal?.aborted) {
                return;
              }
              setContent(content => ({chunks: [missingLines.map((currentValue, i) => convert.toHtml(currentValue.line)), ...content.chunks], content: content.content}));
            } else if(logs.status !== 204) {
              throw new Error(`Unexpected http error: ${logs.status}`);
            }
            setError("");
          } catch(ex) {
            if(ex instanceof Object) {
              setError(ex.toString());
            } else {
              setError("Unknown Error: " + ex);
            }
          } finally {
            setLoading(false)
          }
        }
      })();
    }
  }, [prop.timeline, prop.render, abortcontroller]);
  return (<span key={prop.timeline?.id} style={{display: (prop.render === undefined || prop.render) ? "block" : "none"}}>
    <MemoChunks chunks={content.chunks}></MemoChunks>
    {(loading ? [ "Loading..." ] : (error && [ error ]) || content.content || [ "Loading..." ]).map((line, i) =>(
      <span key={i} style={{textAlign: 'left', whiteSpace: 'pre-wrap', display: "block", overflow: 'auto', fontFamily: "SFMono-Regular,Consolas,Liberation Mono,Menlo,monospace"}} dangerouslySetInnerHTML={{ __html: line }}/>
    ))}
  </span>);
};

interface IWorkflowRunAttempt {
  attempt: string,
  fileName: string,
  timeLineId: string
}

const TimelineStatus = ({status, size} : { status : string, size?: number }) => {
  switch(status?.toLowerCase()) {
    case "inprogress":
      return <MeterIcon className="text-warning progress-ring" size={size}/>
    case "waiting":
    case "pending":
      return <CircleIcon verticalAlign="middle" size={size}/>
    case "succeeded":
      return <CheckCircleFillIcon className="text-success" verticalAlign="middle" size={size}/>
    case "failed":
      return <XCircleFillIcon className="text-danger" verticalAlign="middle" size={size}/>
    case "skipped":
      return <SkipIcon verticalAlign="middle" size={size}/>
    case "canceled":
      return <StopIcon verticalAlign="middle" size={size}/>
    default:
      return <span>{status}</span>
  }
};

interface CollapsibleProps {
  timelineEntry : ITimeLine,
  registerLiveLog: (recordId: string, callback: (line: string[]) => void) => void
  unregisterLiveLog: (recordId: string) => void
}
const Collapsible : React.FC<CollapsibleProps> = props => {
  const [open, setOpen] = useState<boolean>(false);
  const [implicitOpen, setImplicitOpen] = useState<boolean>(false);
  return (<div className='mt-1 mb-1' style={{display: "block"}}><button tabIndex={0} onClick={() => setOpen(open => !open)} className={open ? 'd-flex btn btn-secondary text-start w-100 active' : 'd-flex btn btn-secondary text-start w-100'}><span style={{width: "100%"}}>{open ? (<ChevronDownIcon/>) : (<ChevronRightIcon/>)} <TimelineStatus status={props.timelineEntry.result ?? props.timelineEntry.state ?? "Waiting" }/> {props.timelineEntry.name}</span><span style={{alignSelf: 'flex-end', flexShrink: "0"}}></span></button>{(<Detail render={open === undefined ? implicitOpen : open} timeline={props.timelineEntry} registerLiveLog={(recordId, callback) => {
    props.registerLiveLog(recordId, line => {
      var impl = implicitOpen;
      if(impl) {
        // Opening the log component causes a load request, which leads to duplicated lines.
        // Skipping the first live log events reduces the chance to see this.
        callback(line);
      } else {
        setImplicitOpen(true);
        setOpen(true);
      }
    });
  }} unregisterLiveLog={recordId => props.unregisterLiveLog(recordId)}/>)}</div>)
}

interface IWorkflowRun {
  id: string,
  fileName: string,
  owner?: string,
  repo?: string,
  displayName: string,
  eventName: string,
  ref: string,
  sha: string,
  result: string
}
function JobPage() {
  var params = useParams();
  const [job, setJob] = useState<IJob>();
  const [workflowRunAttempt, setWorkflowRunAttempt] = useState<IWorkflowRunAttempt>();
  const [workflowRun, setWorkflowRun] = useState<IWorkflowRun>();
  const [timeline, setTimeline] = useState<ITimeLine[]>();
  const eventHandler = useMemo<Map<string, (line: string[]) => void>>(() => new Map(), []);
  const [ artifacts, setArtifacts ] = useState<ArtifactResponse[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>("");
  useEffect(() => {
    setJob(undefined);
    setTimeline(undefined);
    setWorkflowRunAttempt(undefined);
    setWorkflowRun(undefined);
    setArtifacts([]);
    if(!params.id) {
      (async () => {
        try {
          setLoading(true);
          var runid = params.runid;
          var attempt = 1;
          var owner = params.owner || "";
          var repo = params.repo || "";
          if(runid) {
            var resp = await fetch(`${ghHostApiUrl}/_apis/v1/Message/workflow/run/${runid}/attempt/${attempt}?owner=${encodeURIComponent(owner)}&repo=${encodeURIComponent(repo)}`, { })
            if(resp.status === 200) {
              var workflowRunAttempt : IWorkflowRunAttempt | null = await resp.json();
              setWorkflowRunAttempt(workflowRunAttempt || undefined);
            }
            resp = await fetch(`${ghHostApiUrl}/_apis/v1/Message/workflow/run/${runid}?owner=${encodeURIComponent(owner)}&repo=${encodeURIComponent(repo)}`, { })
            if(resp.status === 200) {
              var workflowRun : IWorkflowRun | null = await resp.json();
              setWorkflowRun(workflowRun || undefined);
            }
            var artifacts = await listArtifacts(Number.parseInt(runid || "1"));
            if(artifacts.value !== undefined) {
                for (let i = 0; i < artifacts.count; i++) {
                    const element = artifacts.value[i];
                    var items = await getContainerItems(element.name, element.fileContainerResourceUrl)
                    if(items !== undefined) {
                        element.files = items.value 
                    }
                }
                setArtifacts(_ => artifacts.value);
            }
            setError("");
          }
        } catch(ex) {
          if(ex instanceof Object) {
            setError(ex.toString());
          } else {
            setError("Unknown Error: " + ex);
          }
        } finally {
          setLoading(false);
        }
      })()
    } else if(params.id) {
      (async () => {
        var resp = await fetch(ghHostApiUrl + "/_apis/v1/Message?jobid=" + encodeURIComponent(params.id || ""), { })
        if(resp.status === 200) {
          var job : IJob | null = await resp.json();
          setJob(job || undefined);
        }
      })()
    }
  }, [params.id, params.owner, params.repo, params.runid]);
  useEffect(() => {
    var timeLineId = job?.timeLineId || workflowRunAttempt?.timeLineId;
    if(timeLineId) {
      var source = new EventSource(ghHostApiUrl + "/_apis/v1/TimeLineWebConsoleLog?timelineId="+ timeLineId);
      source.addEventListener("log", (me) => {
        var ev = me as MessageEvent;
        var e = JSON.parse(ev.data) as ILoglineEvent;
        eventHandler.get(e.record.stepId)?.call(undefined, e.record.value);
      });
      source.addEventListener ("timeline", (me : Event) => {
        var ev = me as MessageEvent;
        var e = JSON.parse(ev.data) as ITimeLineEvent;
        setTimeline(_oldtimeline => {
          e.timeline.forEach(entry => entry.timelineId = timeLineId);
          return e.timeline;
        });
      });
      source.addEventListener ("finish", (me : Event) => {
        var ev = me as MessageEvent;
        var e = JSON.parse(ev.data) as IJobCompletedEvent;
        if(e.jobId === job?.jobId) {
          setJob(oldjob => {
            if(oldjob && !oldjob.result) {
              var job : IJob = {...oldjob};
              job.result = e.result;
              return job;
            }
            return oldjob;
          });
        }
      });
      (async () => {
        var resp = await fetch(ghHostApiUrl + "/_apis/v1/Timeline/" + timeLineId, { });
        if(resp.status === 200) {
            var newTimeline = await resp.json() as ITimeLine[];
            if(newTimeline != null && newTimeline.length > 0) {
                newTimeline.forEach(entry => entry.timelineId = timeLineId);
                setTimeline(newTimeline);
            } else {
                setTimeline([]);
            }
        } else {
            setTimeline([]);
        }
      })()
      return () => {
        source.close();
      }
    }
  }, [job?.jobId, job?.timeLineId, workflowRunAttempt?.timeLineId, eventHandler]);
  const [summaries, setSummaries] = useState<string[]>([])
  useEffect(() => {
    setSummaries([]);
    var signal = new AbortController();
    (async() => {
      for(var summary of artifacts.filter(artifact => artifact.files !== undefined && /Attachment_[^_]+_[^_]+_Checks\.Step\.Summary/.test(artifact.name)).flatMap((container: ArtifactResponse) => (container.files || []).filter(f => f.itemType === "file").map(file => file.contentLocation))) {
        if(signal.signal.aborted) return;
        var resp = await fetch(summary, { signal: signal.signal })
        if(signal.signal.aborted) return;
        if(resp.status === 200) {
          var text = await resp.text();
          if(signal.signal.aborted) return;
          setSummaries((text => summaries => [...summaries, text])(text));
        }
      }
    })();
    return () => signal.abort();
  }, [artifacts]);
  return ( <span style={{width: '100%', height: '100%', overflowY: 'auto'}}>
    <h1>{workflowRun ? workflowRun.fileName : job ? (<><TimelineStatus status={job?.result ?? "inprogress"} size={32}/> {job.name}</>) : ""}</h1>
    {(() => {
      if(job !== undefined && job != null) {
          if(!job.result && (!job.errors || job.errors.length === 0)) {
              return <div className="btn-group" role="group">
                  <button className='btn btn-secondary' onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/cancelWorkflow/" + job.runid, { method: "POST" });
                      })();
                  }}>Cancel Workflow</button>
                  <button className='btn btn-secondary' onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/cancel/" + job.jobId, { method: "POST" });
                      })();
                  }}>Cancel</button>
                  <button className='btn btn-secondary' onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/cancel/" + job.jobId + "?force=true", { method: "POST" });
                      })();
                  }}>Force Cancel</button>
              </div>;
          } else {
              return <div className="btn-group" role="group">
                  <button className='btn btn-secondary' onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunworkflow/" + job.runid, { method: "POST" });
                      })();
                  }}>Rerun Workflow</button>
                  <button className='btn btn-secondary' onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunFailed/" + job.runid, { method: "POST" });
                      })();
                  }}>Rerun Failed Jobs</button>
                  <button className='btn btn-secondary' onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerun/" + job.jobId, { method: "POST" });
                      })();
                  }}>Rerun</button>
                  <button className='btn btn-secondary' onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunworkflow/" + job.runid + "?onLatestCommit=true", { method: "POST" });
                      })();
                  }}>Rerun Workflow (&nbsp;Latest&nbsp;Commit&nbsp;)</button>
                  <button className='btn btn-secondary' onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunFailed/" + job.runid + "?onLatestCommit=true", { method: "POST" });
                      })();
                  }}>Rerun Failed Jobs (&nbsp;Latest&nbsp;Commit&nbsp;)</button>
                  <button className='btn btn-secondary' onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerun/" + job.jobId + "?onLatestCommit=true", { method: "POST" });
                      })();
                  }}>Rerun (&nbsp;Latest&nbsp;Commit&nbsp;)</button>
              </div>;
          }
      } else if(workflowRun !== undefined && workflowRun != null) {
        return (<><div className="btn-group" role="group">
        <button className='btn btn-secondary' onClick={(event) => {
          (async () => {
              await fetch(ghHostApiUrl + "/_apis/v1/Message/cancelWorkflow/" + params.runid, { method: "POST" });
          })();
        }}>Cancel Workflow</button>
        <button className='btn btn-secondary' onClick={(event) => {
          (async () => {
              await fetch(ghHostApiUrl + "/_apis/v1/Message/forceCancelWorkflow/" + params.runid, { method: "POST" });
          })();
        }}>Force Cancel Workflow</button>
        <button className='btn btn-secondary' onClick={(event) => {
            (async () => {
                await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunworkflow/" + params.runid, { method: "POST" });
            })();
        }}>Rerun Workflow</button>
        <button className='btn btn-secondary' onClick={(event) => {
            (async () => {
                await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunFailed/" + params.runid, { method: "POST" });
            })();
        }}>Rerun Failed Jobs</button>
        <button className='btn btn-secondary' onClick={(event) => {
            (async () => {
                await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunworkflow/" + params.runid + "?onLatestCommit=true", { method: "POST" });
            })();
        }}>Rerun Workflow (&nbsp;Latest&nbsp;Commit&nbsp;)</button>
        <button className='btn btn-secondary' onClick={(event) => {
            (async () => {
                await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunFailed/" + params.runid + "?onLatestCommit=true", { method: "POST" });
            })();
        }}>Rerun Failed Jobs (&nbsp;Latest&nbsp;Commit&nbsp;)</button>
        </div>
        {artifacts.map((container: ArtifactResponse) => <div>{(() => {
            if(container.files !== undefined) {
                return (<div>{(container.files || []).filter(f => f.itemType === "file").map(file => <div><a className="btn btn-outline-secondary w-100 text-start" href={file.contentLocation}>{file.path}</a></div>)}</div>);
            }
            return <div/>;
        })()}</div>)}
        </>);
      }
  })()
  }
    { loading ? <span>Loading...</span> : error ? <span>{error}</span> : <></> }
    {(() => {
      if((timeline?.length || 0) > 1) {
        return (<>{timeline?.map((timelineEntry, i) => (<Collapsible key={timelineEntry.id} timelineEntry={timelineEntry} registerLiveLog={(recordId, callback) => eventHandler.set(recordId, callback)} unregisterLiveLog={recordId => eventHandler.delete(recordId)}></Collapsible>))}</>);
      }
      return (<Detail timeline={(timeline || [null])[0]} render={true} registerLiveLog={(recordId, callback) => eventHandler.set(recordId, callback)} unregisterLiveLog={recordId => eventHandler.delete(recordId)}/>)
    })()
  }
  {<h2>{summaries.length > 0 && "Comment summary"}</h2>}
  {summaries.map((content, i) => <ReactMarkdown className='markdown-body' key={"summary-"+ i} remarkPlugins={[remarkGfm, remarkBreaks]} rehypePlugins={[rehypeHighlight, rehypeRaw]} >{content}</ReactMarkdown>)}
  </span>);
}

interface IOwner {
    name: string,
}

interface IRepository {
  name: string,
}

interface IWorkflowRun {
  id: string,
  fileName: string
}

function RedirectOldUrl() {
  const [url, setUrl] = useState<string>();
  var params = useParams();
  useEffect(() => {
    (async () => {
      var resp = await fetch(ghHostApiUrl + "/_apis/v1/Message?jobid=" + encodeURIComponent(params.id || ""), { })
      if(resp.status === 200) {
        var job : IJob | null = await resp.json();
        if(job) {
          var ownerrepo = job.repo.split("/");
          setUrl(`/0/${ownerrepo[0]}/0/${ownerrepo[1]}/0/${job.runid}/0/${job.jobId}`);
          return;
        }
      }
      setUrl("/0");
    })()
  });
  if(url) {
    return <Navigate to={url}/>;
  }
  return <div>Redirecting...</div>
}

function TimeLineViewer() {
  var params = useParams();
  const [timeline, setTimeline] = useState<ITimeLine[]>();
  const eventHandler = useMemo<Map<string, (line: string[]) => void>>(() => new Map(), []);
  useEffect(() => {
    var timeLineId = params.timeLineId;
    if(timeLineId) {
      var source = new EventSource(ghHostApiUrl + "/_apis/v1/TimeLineWebConsoleLog?timelineId="+ timeLineId);
      source.addEventListener("log", (me) => {
        var ev = me as MessageEvent;
        var e = JSON.parse(ev.data) as ILoglineEvent;
        eventHandler.get(e.record.stepId)?.call(undefined, e.record.value);
      });
      source.addEventListener ("timeline", (me : Event) => {
        var ev = me as MessageEvent;
        var e = JSON.parse(ev.data) as ITimeLineEvent;
        setTimeline(_oldtimeline => {
          e.timeline.forEach(entry => entry.timelineId = timeLineId);
          return e.timeline;
        });
      });
      (async () => {
        var resp = await fetch(ghHostApiUrl + "/_apis/v1/Timeline/" + timeLineId, { });
        if(resp.status === 200) {
            var newTimeline = await resp.json() as ITimeLine[];
            if(newTimeline != null && newTimeline.length > 0) {
                newTimeline.forEach(entry => entry.timelineId = timeLineId);
                setTimeline(newTimeline);
            } else {
                setTimeline([]);
            }
        } else {
            setTimeline([]);
        }
      })()
      return () => {
        source.close();
      }
    }
  }, [params.timeLineId, eventHandler]);
  return ( <span style={{width: '100%', height: '100%', overflowY: 'auto'}}>
    {(() => {
      if((timeline?.length || 0) > 1) {
        return (<>{timeline?.map((timelineEntry, i) => (<Collapsible key={timelineEntry.id} timelineEntry={timelineEntry} registerLiveLog={(recordId, callback) => eventHandler.set(recordId, callback)} unregisterLiveLog={recordId => eventHandler.delete(recordId)}></Collapsible>))}</>);
      }
      return (<Detail timeline={(timeline || [null])[0]} render={true} registerLiveLog={(recordId, callback) => eventHandler.set(recordId, callback)} unregisterLiveLog={recordId => eventHandler.delete(recordId)}/>)
    })()
  }
  </span>);
}

function AllJobs() {
  return (<div style={{display: 'flex', flexFlow: 'row', alignItems: 'left', width: '100%', height: '100%'}}>
    <Routes>
      <Route path=":page/*" element={<List/>}/>
      <Route path=":page/" element={<List fullscreen={true}/>}/>
      <Route path="/" element={<Navigate to={"0"}/>}/>
    </Routes>
    <Routes>
      <Route path=":page/:id/*" element={<JobPage></JobPage>}/>
      <Route path=":page" element={<></>}/>
    </Routes>
  </div>);
}

function App() {
  var [gitServerUrl, setGitServerUrl] = useState<string>();
  //let [searchParams, setSearchParams] = useSearchParams();
  var searchParams = new URLSearchParams(window.location.search)
  useEffect(() => {
    (async () => {
      var resp = await fetch(ghHostApiUrl + "/_apis/v1/Message/gitserverurl");
      if(resp.status === 200) {
        setGitServerUrl(await resp.text())
      }
    })()
  }, []);
  return searchParams.get("view") === "alljobs" ?
    ( <AllJobs/> ) :
    searchParams.get("view") === "allworkflows" ?
    ( <Routes>
      <Route path=":page" element={<GenericList hasBack={false} id={(o: IWorkflowRun) => o.id} summary={(o: IWorkflowRun) => <span>{o.displayName ?? o.fileName}<br/>{ o.owner && o.repo ? <>Repository: {o.owner}/{o.repo} </> : <></>}RunId: {o.id}, EventName: {o.eventName}<br/>Workflow: {o.fileName}<br/>{o.ref} {o.sha} <TimelineStatus status={o.result ?? "Pending"}/></span>} url={(params) => `${ghHostApiUrl}/_apis/v1/Message/workflow/runs?page=${params.page || "0"}`} eventName="workflowrun" eventUpdateName="workflowrunupdate" eventQuery={ params => `owner=${encodeURIComponent(params.owner || "")}&repo=${encodeURIComponent(params.repo || "")}` }></GenericList>}/>
      <Route path="/" element={<Navigate to={"0"}/>}/>
      <Route path=":page/:runid/*" element={
        <div style={{display: 'flex', flexFlow: 'row', alignItems: 'left', width: '100%', height: '100%'}}>
          <Routes>
            <Route path=":page/*" element={<List/>}/>
            <Route path="/" element={<Navigate to={"0"}/>}/>
          </Routes>
          <Routes>
            <Route path=":page/:id/*" element={<JobPage></JobPage>}/>
            <Route path=":page" element={<JobPage></JobPage>}/>
          </Routes>
        </div>
      }/>
    </Routes> ) :
    (
      <Routes>
        <Route path="/timeline/:timeLineId" element={<TimeLineViewer></TimeLineViewer>}/>
        <Route path="/master/:a/:b/detail/:id" element={<RedirectOldUrl/>}/>
        <Route path="/master" element={<Navigate to={"0"}/>}/>
        <Route path=":page" element={<GenericList id={(o: IOwner) => o.name} summary={(o: IOwner) => <div style={{padding: "10px"}}>{o.name}</div>} url={(params) => ghHostApiUrl + "/_apis/v1/Message/owners?page=" + (params.page || "0")} externalBackUrl={params => gitServerUrl} externalBackLabel={() => "Back to git"} actions={ o => gitServerUrl ? <a className='btn btn-outline-secondary' href={new URL(o.name, gitServerUrl).href} target="_blank" rel="noreferrer"><PersonIcon size={24}/></a> : <></> } eventName="owner" eventQuery={ params => "" }></GenericList>}/>
        <Route path="/" element={<Navigate to={"0"}/>}/>
        <Route path=":page/:owner/*" element={
          <Routes>
            <Route path=":page" element={<GenericList id={(o: IRepository) => o.name} hasBack={true} summary={(o: IRepository) => <div style={{padding: "10px"}}>{o.name}</div>} url={(params) => `${ghHostApiUrl}/_apis/v1/Message/repositories?owner=${encodeURIComponent(params.owner || "zero")}&page=${params.page || "0"}`} externalBackUrl={params => gitServerUrl && new URL(`${params.owner}`, gitServerUrl).href} externalBackLabel={() => "Back to git"} actions={ (r, params) => gitServerUrl ? <a className='btn btn-outline-secondary' href={new URL(`${params.owner}/${r.name}`, gitServerUrl).href} target="_blank" rel="noreferrer"><RepoIcon size={24}/></a> : <></> } eventName="repo" eventQuery={ params => `owner=${encodeURIComponent(params.owner || "")}` }></GenericList>}/>
            <Route path="/" element={<Navigate to={"0"}/>}/>
            <Route path=":page/:repo/*" element={
              <Routes>
                <Route path=":page" element={<GenericList id={(o: IWorkflowRun) => o.id} hasBack={true} summary={(o: IWorkflowRun) => <span>{o.displayName ?? o.fileName}<br/>RunId: {o.id}, EventName: {o.eventName}<br/>Workflow: {o.fileName}<br/>{o.ref} {o.sha} <TimelineStatus status={o.result ?? "Pending"}/></span>} url={(params) => `${ghHostApiUrl}/_apis/v1/Message/workflow/runs?owner=${encodeURIComponent(params.owner || "")}&repo=${encodeURIComponent(params.repo || "")}&page=${params.page || "0"}`} externalBackUrl={params => gitServerUrl && new URL(`${params.owner}/${params.repo}`, gitServerUrl).href} externalBackLabel={() => "Back to git"} actions={ (run, params) => gitServerUrl ? <a className='btn btn-outline-secondary' href={new URL(`${params.owner}/${params.repo}/commit/${run.sha}`, gitServerUrl).href} target="_blank" rel="noreferrer"><GitCommitIcon verticalAlign='middle' size={24}/></a> : <></> } eventName="workflowrun" eventUpdateName="workflowrunupdate" eventQuery={ params => `owner=${encodeURIComponent(params.owner || "")}&repo=${encodeURIComponent(params.repo || "")}` }></GenericList>}/>
                <Route path="/" element={<Navigate to={"0"}/>}/>
                <Route path=":page/:runid/*" element={
                  <div style={{display: 'flex', flexFlow: 'row', alignItems: 'left', width: '100%', height: '100%'}}>
                    <Routes>
                      <Route path=":page/*" element={<List/>}/>
                      <Route path="/" element={<Navigate to={"0"}/>}/>
                    </Routes>
                    <Routes>
                      <Route path=":page/:id/*" element={<JobPage></JobPage>}/>
                      <Route path=":page" element={<JobPage></JobPage>}/>
                    </Routes>
                  </div>
                }/>
              </Routes>
            }/>
          </Routes>
        }/>
      </Routes>
    );
}

export default App;
