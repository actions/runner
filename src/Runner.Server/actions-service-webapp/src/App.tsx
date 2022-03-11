import React, { useEffect, useMemo, useState } from 'react';
import './App.css';
import { Link, Navigate, NavLink, Route, Routes, useParams, Params, useResolvedPath, resolvePath } from 'react-router-dom';
import Convert from 'ansi-to-html'; 
var convert = new Convert({
  newline: true,
  escapeXML: true
});

interface IJobEvent {
  repo: string,
  job: IJob 
}

function List() {
  var params = useParams();
  const [ jobs, setJobs ] = useState<IJob[]>([]);
  var page = Number.parseInt(params["page"] || "0");
  var parentPath = useResolvedPath("..");
  var resolved = resolvePath("..", parentPath.pathname);
  useEffect(() => {
    setJobs([]);
    if(params.page) {
      (async () => {
        var resp = await fetch(`${ghHostApiUrl}/_apis/v1/Message?page=${encodeURIComponent(page)}&repo=${encodeURIComponent(params.owner + "/" + params.repo)}&runid=${encodeURIComponent(params.runid || "null")}`, { })
        if(resp.status === 200) {
          var jobs : IJob[] | null = await resp.json();
          setJobs(jobs || []);
        }
      })()
      if(page === 0) {
        var source = new EventSource(`${ghHostApiUrl}/_apis/v1/Message/event?filter=${encodeURIComponent(params.owner + "/" + params.repo)}&runid=${encodeURIComponent(params.runid || "null")}`);
        source.addEventListener("job", ev => {
          var je = JSON.parse((ev as MessageEvent).data) as IJobEvent;
          var x = je.job;
          setJobs(_jobs => {
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
        return () => source.close();
      }
    }
  }, [page, params.page, params.owner, params.repo, params.runid]);
  return (<span style={{
      maxWidth: '400px',
      width: '40vw',
      height: '100%',
      overflowY: 'auto',
      border: '0',
      borderRight: '1px',
      borderColor: 'gray',
      borderStyle: 'solid'}}>
    <Link style={{width: "calc(100% - 22px)", color: 'black', textDecoration: "none", display: "block",
      border: '1px',
      borderBottom: '0',
      borderColor: 'gray',
      borderStyle: 'solid',
      padding: '10px'}} to={resolved}>Back</Link>
    <Link  to={"."} style={{
        width: 'calc(100% - 22px)',
        display: "block",
        border: "1px",
        borderStyle: 'solid',
        borderColor: 'gray',
        padding: '10px',
        color: 'black',
        textDecoration: 'none',
        background: "white"
      }}>Summary</Link>
    <div style={{
      display: "flex",
      width: 'calc(100% - 2px)',
      // height: '1px',
      border: '1px',
      borderTop: '0',
      borderColor: 'gray',
      borderStyle: 'solid'
    }}>
      <Link style={{width: "50%", color: 'black', textDecoration: "none", visibility: page <= 0 ? "collapse" : "visible", padding: '10px' }} to={"../"+ (page - 1)  + "/" + params['*']}>Previous</Link>
      <Link style={{width: "50%", color: 'black', textDecoration: "none", padding: '10px'}} to={"../"+ (page + 1)  + "/" + params['*']}>Next</Link>
    </div>
    {/* <span style={{
      display: "block",
      width: '100%',
      height: '1px',
      backgroundColor: 'gray'
    }}></span> */}
    {jobs.map(val => (
      <NavLink key={val.jobId} to={encodeURIComponent(val.jobId)} style={({ isActive }) => {
        return {
          width: 'calc(100% - 2px)',
          display: "block",
          borderLeft: "1px",
          borderRight: "1px",
          borderBottom: "1px",
          borderTop: "0",
          borderStyle: 'solid',
          borderColor: 'gray',
          // margin: "1rem 0",
          color: 'black',
          textDecoration: 'none',
          background: isActive ? "lightblue" : "white"
        };
      }}><span style={{fontSize: 20}}>{val.name}</span><br/><span style={{fontSize: 12}}>{val.workflowname}</span><br/><span style={{fontSize: 12}}>runid:&nbsp;{val.runid} attempt:&nbsp;{val.attempt} result:&nbsp;{val.result}</span></NavLink>
    ))}
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
  eventQuery?: (params : Readonly<Params<string>>) => string
}

const GenericList = <T, >(param : GenericListProps<T>) => {
  var parentPath = useResolvedPath("..");
  var resolved = resolvePath("..", parentPath.pathname);
  var params = useParams();
  const [ jobs, setJobs ] = useState<T[]>([]);
  useEffect(() => {
    setJobs([]);
    if(params.page) {
      (async () => {
        var resp = await fetch(param.url(params), { })
        if(resp.status === 200) {
          var jobs : T[] | null = await resp.json();
          setJobs(jobs || []);
        }
      })()
      if(page === 0 && param.eventName) {
        var source = new EventSource(`${ghHostApiUrl}/_apis/v1/Message/event2?${(param.eventQuery && param.eventQuery(params)) ?? ""}`);
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
        return () => source.close();
      }
    }
  }, [params, param]);
  var page = Number.parseInt(params["page"] || "0");
  return (<div style={{width: "100%", height: "100%", overflowY: 'auto'}}>
    <Link style={{width: "calc(100% - 22px)", color: 'black', textDecoration: "none", display: !param.hasBack ? "none" : "block",
      border: '1px',
      borderBottom: '0',
      borderColor: 'gray',
      borderStyle: 'solid',
      padding: '10px' }} to={resolved}>Back</Link>
    {param.externalBackUrl && param.externalBackLabel && param.externalBackLabel(params) ? <a style={{width: "calc(100% - 22px)", color: 'black', textDecoration: "none", display: !param.externalBackUrl ? "none" : "block",
      border: '1px',
      borderBottom: '0',
      borderColor: 'gray',
      borderStyle: 'solid',
      padding: '10px' }} href={param.externalBackUrl(params) || ""} target="_blank" rel="noreferrer">{param.externalBackLabel(params)}</a>: <></>}
    <div style={{
      display: "flex",
      width: 'calc(100% - 2px)',
      // height: '1px',
      border: '1px',
      borderTop: '1px',
      borderColor: 'gray',
      borderStyle: 'solid'
    }}>
      <Link style={{width: "50%", color: 'black', textDecoration: "none", visibility: page <= 0 ? "collapse" : "visible", padding: '10px' }} to={"../"+ (page - 1)}>Previous</Link>
      <Link style={{width: "50%", color: 'black', textDecoration: "none", padding: '10px'}} to={"../"+ (page + 1)}>Next</Link>
    </div>
    {/* <span style={{
      display: "block",
      width: '100%',
      height: '1px',
      backgroundColor: 'gray'
    }}></span> */}
    {jobs.map(val => (
      <div key={param.id(val)} style={{
        width: 'calc(100% - 2px)',
        display: "flex",
        borderLeft: "1px",
        borderRight: "1px",
        borderBottom: "1px",
        borderTop: "0",
        borderStyle: 'solid',
        borderColor: 'gray',
        // margin: "1rem 0",
      }} >
        <NavLink to={`${encodeURIComponent(param.id(val))}/0`} style={({ isActive }) => {
          return {
            width: "100%",
            textDecoration: 'none',
            color: 'black',
            background: isActive ? "lightblue" : "white"
          };
        }}>{param.summary(val, params)}</NavLink>
        {(param.actions && param.actions(val, params)) || ""}
      </div>
      
    ))}
  </div>);
};

var ghHostApiUrl = "";

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
  // const [chunks, setChunks] = useState<string[][]>([]);
  // const [content, setContent] = useState<string[]>([]);
  const [content, setContent] = useState<Content>({chunks: [], content: []});
  // const [loading, setLoading] = useState<boolean>(false);
  // const [loaded, setLoaded] = useState<boolean>(false);
  useEffect(() => {
    setContent({chunks: [], content: []});
  }, [prop.timeline?.id]);
  useEffect(() => {
    // setLoaded(false);
    if(!prop.timeline) {
      return;
    }
    prop.registerLiveLog(prop.timeline.id || "", lines => {
      // var movedToChunk : string[] = [];
      setContent(content => {
        if(content.content.length > 500) {
          return {chunks: [...content.chunks, [...content.content]], content: [...lines.map(line => convert.toHtml(line))]};
        }
        return {chunks: [...content.chunks], content: [...content.content, ...lines.map(line => convert.toHtml(line))]};
        // if(content.content.length > 100) {
        //   content.chunks.push([...content.content]);
        //   return {chunks: content.chunks, content: [...lines]};
        // }
        // return {chunks: content.chunks, content: [...content.content, ...lines]};
        // var _content = content || [];
        // if(_content.length > 100) {
        //   if(movedToChunk.length === 0) {
        //     movedToChunk =  [..._content];
        //     setTimeout(() => {
        //       setChunks(chunks => {
        //         return [...chunks, movedToChunk];
        //       });
        //     }, 0);
        //   }
        //   if(_content.length !== movedToChunk.length) {
        //     console.log("What happend?");
        //   }
        //   return _content.slice(movedToChunk.length);
        // }
        // return [...(_content), ...lines.map(line => convert.toHtml(line))];
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
          var response = await fetch(ghHostApiUrl + "/_apis/v1/Logfiles/" + prop.timeline?.log?.id, { signal });
          if(response.status !== 200) {
            // return setContent(["Failed to load Log"]);
            return setContent(content => ({chunks: [["Failed to load Log"], ...content.chunks], content: content.content}));
          }
          const log = await response.text();
          var lines = log.split(/\r?\n/);
          var offset = '2021-04-02T15:50:14.6619714Z '.length;
          var re = /^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}.[0-9]{7}Z /;
          // setChunks(chunks => [lines.map((currentValue, i) => convert.toHtml(re.test(currentValue) ? /* new Date(currentValue.substring(0, offset - 1)).toUTCString() + " " +  */ /* (i + 1).toString().padStart(5) + " " + */ currentValue.substring(offset) : currentValue)), ...chunks]);
          // setContent(content => ({chunks: [lines.map((currentValue, i) => convert.toHtml(re.test(currentValue) ? /* new Date(currentValue.substring(0, offset - 1)).toUTCString() + " " +  */ /* (i + 1).toString().padStart(5) + " " + */ currentValue.substring(offset) : currentValue)), ...content.chunks], content: content.content}));
          var j = 0;
          if(signal?.aborted) {
            return;
          }
          var registr = setInterval(() => {
            var i = j;
            var len = Math.min(lines.length - i * 5000, 5000);
            if(len <= 0 || signal?.aborted) {
              clearInterval(registr);
              return;
            }
            setContent(content => ({chunks: [...content.chunks.slice(0, i), lines.slice(i * 5000, i * 5000 + len).map((currentValue, i) => convert.toHtml(re.test(currentValue) ? /* new Date(currentValue.substring(0, offset - 1)).toUTCString() + " " +  */ /* (i + 1).toString().padStart(5) + " " + */ currentValue.substring(offset) : currentValue)), ...content.chunks.slice(i)], content: content.content}));
            j++;
          }, 1000);
          signal?.addEventListener("abort", () => clearInterval(registr));
          // setLoaded(true);
        } else if(prop.timeline?.id && prop.timeline?.timelineId) {
          var logs = await fetch(ghHostApiUrl + "/_apis/v1/TimeLineWebConsoleLog/" + prop.timeline?.timelineId + "/" + prop.timeline?.id, { signal });
          if(logs.status === 200) {
            var missingLines = await logs.json() as ILogline[];
            if(signal?.aborted) {
              return;
            }
            // setChunks(chunks => [missingLines.map((currentValue, i) => convert.toHtml(currentValue.line)), ...chunks]);
            setContent(content => ({chunks: [missingLines.map((currentValue, i) => convert.toHtml(currentValue.line)), ...content.chunks], content: content.content}));
            // setLoaded(true);
          }
        }
      })();
    }
  }, [prop.timeline, prop.render, abortcontroller]);
  return (<span key={prop.timeline?.id} style={{display: (prop.render === undefined || prop.render) ? "block" : "none"}}>
    <MemoChunks chunks={content.chunks}></MemoChunks>
    {(content.content || [ "Loading..." ]).map((line, i) =>(
      <span key={i} style={{textAlign: 'left', whiteSpace: 'pre-wrap', display: "block", overflow: 'auto', fontFamily: "SFMono-Regular,Consolas,Liberation Mono,Menlo,monospace"}} dangerouslySetInnerHTML={{ __html: line }}/>
    ))}
  </span>);
  // if(prop.render === undefined || prop.render) {
  //   return (<span>
  //     {/* {chunks.flatMap((chunk, j) => chunk.map((line, i) =>(
  //       <span key={prop.timeline?.id+ "-" + j.toString()  + "-" + i.toString()} style={{textAlign: 'left', whiteSpace: 'pre-wrap', display: "block", overflow: 'auto', fontFamily: "SFMono-Regular,Consolas,Liberation Mono,Menlo,monospace"}} dangerouslySetInnerHTML={{ __html: line }}/>
  //     )))} */}
  //     {/* <MemoChunks chunks={chunks}></MemoChunks>
  //     <b>Name: {prop.timeline?.name}</b><br/>
  //     {(content || [ "Loading..." ]).map((line, i) =>(
  //       <span key={prop.timeline?.id + "-" + i.toString()} style={{textAlign: 'left', whiteSpace: 'pre-wrap', display: "block", overflow: 'auto', fontFamily: "SFMono-Regular,Consolas,Liberation Mono,Menlo,monospace"}} dangerouslySetInnerHTML={{ __html: line }}/>
  //     ))} */}

  //     <MemoChunks chunks={content.chunks}></MemoChunks>
  //     {(content.content || [ "Loading..." ]).map((line, i) =>(
  //       <span key={prop.timeline?.id + "-" + i.toString()} style={{textAlign: 'left', whiteSpace: 'pre-wrap', display: "block", overflow: 'auto', fontFamily: "SFMono-Regular,Consolas,Liberation Mono,Menlo,monospace"}} dangerouslySetInnerHTML={{ __html: line }}/>
  //     ))}
  //     {/* <b>Result: {prop.timeline?.result}</b><br/> */}
  //   </span>);
  // }
  // return (<></>);
};

interface IWorkflowRunAttempt {
  attempt: string,
  fileName: string,
  timeLineId: string
}

interface CollapsibleProps {
  timelineEntry : ITimeLine,
  registerLiveLog: (recordId: string, callback: (line: string[]) => void) => void
  unregisterLiveLog: (recordId: string) => void
}
const Collapsible : React.FC<CollapsibleProps> = props => {
  const [open, setOpen] = useState<boolean>();
  const [implicitOpen, setImplicitOpen] = useState<boolean>(false);
  return (<div style={{display: "block"}}><div tabIndex={0} onKeyPress={() => setOpen(open => !open)} onClick={() => setOpen(open => !open)} style={{display: "flex", border: "1px", margin: "1px", width: "calc(100% - 4px)", borderStyle: "solid"}}><span style={{width: "100%"}}>{props.timelineEntry.result ?? props.timelineEntry.state ?? "Waiting" } - {props.timelineEntry.name}</span><span style={{alignSelf: 'flex-end', flexShrink: "0"}}>{(open === undefined ? implicitOpen : open) ? (open === undefined ? "Implicit Expanded" : "Expanded"): "Collapsed"}</span></div>{(<Detail render={open === undefined ? implicitOpen : open} timeline={props.timelineEntry} registerLiveLog={(recordId, callback) => {
    props.registerLiveLog(recordId, line => {
      var impl = implicitOpen;
      setImplicitOpen(true);
      if(impl) {
        // Opening the log component causes a load request, which leads to duplicated lines.
        // Skipping the first live log events reduces the chance to see this.
        callback(line);
      }
    });
  }} unregisterLiveLog={recordId => props.unregisterLiveLog(recordId)}/>)}</div>)
}
//key={timelineEntry.id}
interface IWorkflowRun {
  id: string,
  fileName: string,
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
  useEffect(() => {
    setJob(undefined);
    setTimeline(undefined);
    setWorkflowRunAttempt(undefined);
    setWorkflowRun(undefined);
    setArtifacts([]);
    if(!params.id) {
      (async () => {
        // var runid = 2;
        // var attempt = 1;
        // var owner = "ChristopherHX";
        // var repo = "runner.server";
        var runid = params.runid;
        var attempt = 1;
        var owner = params.owner || "";
        var repo = params.repo || "";
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
  return ( <span style={{width: '100%', height: '100%', overflowY: 'auto'}}>
    <h1>{workflowRun ? workflowRun.fileName : job ? (job?.result ? job.name + " completed with result: " + job.result : job?.name) : ""}</h1>
    {(() => {
      if(job !== undefined && job != null) {
          if(!job.result && (!job.errors || job.errors.length === 0)) {
              return <div>
                  <button onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/cancelWorkflow/" + job.runid, { method: "POST" });
                      })();
                  }}>Cancel Workflow</button>
                  <button onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/cancel/" + job.jobId, { method: "POST" });
                      })();
                  }}>Cancel</button>
                  <button onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/cancel/" + job.jobId + "?force=true", { method: "POST" });
                      })();
                  }}>Force Cancel</button>
              </div>;
          } else {
              return <div>
                  <button onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunworkflow/" + job.runid, { method: "POST" });
                      })();
                  }}>Rerun Workflow</button>
                  <button onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunFailed/" + job.runid, { method: "POST" });
                      })();
                  }}>Rerun Failed Jobs</button>
                  <button onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerun/" + job.jobId, { method: "POST" });
                      })();
                  }}>Rerun</button>
                  <button onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunworkflow/" + job.runid + "?onLatestCommit=true", { method: "POST" });
                      })();
                  }}>Rerun Workflow ( Latest Commit )</button>
                  <button onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunFailed/" + job.runid + "?onLatestCommit=true", { method: "POST" });
                      })();
                  }}>Rerun Failed Jobs ( Latest Commit )</button>
                  <button onClick={(event) => {
                      (async () => {
                          await fetch(ghHostApiUrl + "/_apis/v1/Message/rerun/" + job.jobId + "?onLatestCommit=true", { method: "POST" });
                      })();
                  }}>Rerun ( Latest Commit )</button>
              </div>;
          }
      } else if(workflowRun !== undefined && workflowRun != null) {
        return (<><button onClick={(event) => {
          (async () => {
              await fetch(ghHostApiUrl + "/_apis/v1/Message/cancelWorkflow/" + params.runid, { method: "POST" });
          })();
        }}>Cancel Workflow</button>
        <button onClick={(event) => {
          (async () => {
              await fetch(ghHostApiUrl + "/_apis/v1/Message/forceCancelWorkflow/" + params.runid, { method: "POST" });
          })();
        }}>Force Cancel Workflow</button>
        <button onClick={(event) => {
            (async () => {
                await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunworkflow/" + params.runid, { method: "POST" });
            })();
        }}>Rerun Workflow</button>
        <button onClick={(event) => {
            (async () => {
                await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunFailed/" + params.runid, { method: "POST" });
            })();
        }}>Rerun Failed Jobs</button>
        <button onClick={(event) => {
            (async () => {
                await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunworkflow/" + params.runid + "?onLatestCommit=true", { method: "POST" });
            })();
        }}>Rerun Workflow ( Latest Commit )</button>
        <button onClick={(event) => {
            (async () => {
                await fetch(ghHostApiUrl + "/_apis/v1/Message/rerunFailed/" + params.runid + "?onLatestCommit=true", { method: "POST" });
            })();
        }}>Rerun Failed Jobs ( On Latest Commit )</button>
        {artifacts.map((container: ArtifactResponse) => <div>{(() => {
            if(container.files !== undefined) {
                return (<div>{(container.files || []).filter(f => f.itemType === "file").map(file => <div><a href={file.contentLocation}>{file.path}</a></div>)}</div>);
            }
            return <div/>;
        })()}</div>)}
        </>);
      }
  })()
  }
    {(() => {
      if((timeline?.length || 0) > 1) {
        // return (<span style={{width: '100%', height: '100%', overflowY: 'auto'}}>{timeline?.map(timelineEntry => (<Detail key={timelineEntry.id} timeline={timelineEntry} registerLiveLog={(recordId, callback) => eventHandler.set(recordId, callback)} unregisterLiveLog={recordId => eventHandler.delete(recordId)}/>))}</span>);
        // return (<span style={{width: '100%', height: '100%', overflowY: 'auto'}}>{timeline?.map((timelineEntry, i) => (<div key={timelineEntry.id} tabIndex={0} onKeyPress={() => console.log("btn clicked")} onClick={() => console.log("clicked")} style={{display: "block", border: "1px", margin: "1px", width: "calc(100% - 4px)", borderStyle: "solid"}}>{timelineEntry.result} - {timelineEntry.name}</div>))}</span>);
        // return (<>{timeline?.map((timelineEntry, i) => (<Collapsible key={timelineEntry.id} timelineEntry={timelineEntry} content={() => (<Detail key={timelineEntry.id} timeline={timelineEntry} registerLiveLog={(recordId, callback) => eventHandler.set(recordId, callback)} unregisterLiveLog={recordId => eventHandler.delete(recordId)}/>)}></Collapsible>))}</>);
        return (<>{timeline?.map((timelineEntry, i) => (<Collapsible key={timelineEntry.id} timelineEntry={timelineEntry} registerLiveLog={(recordId, callback) => eventHandler.set(recordId, callback)} unregisterLiveLog={recordId => eventHandler.delete(recordId)}></Collapsible>))}</>);
      }
      return (<Detail timeline={(timeline || [null])[0]} render={true} registerLiveLog={(recordId, callback) => eventHandler.set(recordId, callback)} unregisterLiveLog={recordId => eventHandler.delete(recordId)}/>)
    })()
  }</span>);
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

function App() {
  var [gitServerUrl, setGitServerUrl] = useState<string>()
  useEffect(() => {
    (async () => {
      var resp = await fetch(ghHostApiUrl + "/_apis/v1/Message/gitserverurl");
      if(resp.status === 200) {
        setGitServerUrl(await resp.text())
      }
    })()
  }, []);
  return (
      <Routes>
        <Route path="/master/:a/:b/detail/:id" element={<RedirectOldUrl/>}/>
        <Route path="/master" element={<Navigate to={"0"}/>}/>
        <Route path=":page" element={<GenericList id={(o: IOwner) => o.name} summary={(o: IOwner) => <div style={{padding: "10px"}}>{o.name}</div>} url={(params) => ghHostApiUrl + "/_apis/v1/Message/owners?page=" + (params.page || "0")} externalBackUrl={params => gitServerUrl} externalBackLabel={() => "Back to git"} actions={ o => gitServerUrl ? <a href={new URL(o.name, gitServerUrl).href} target="_blank" rel="noreferrer">Git</a> : <></> } eventName="owner" eventQuery={ params => "" }></GenericList>}/>
        <Route path="/" element={<Navigate to={"0"}/>}/>
        <Route path=":page/:owner/*" element={
          <Routes>
            <Route path=":page" element={<GenericList id={(o: IRepository) => o.name} hasBack={true} summary={(o: IRepository) => <div style={{padding: "10px"}}>{o.name}</div>} url={(params) => `${ghHostApiUrl}/_apis/v1/Message/repositories?owner=${encodeURIComponent(params.owner || "zero")}&page=${params.page || "0"}`} externalBackUrl={params => gitServerUrl && new URL(`${params.owner}`, gitServerUrl).href} externalBackLabel={() => "Back to git"} actions={ (r, params) => gitServerUrl ? <a href={new URL(`${params.owner}/${r.name}`, gitServerUrl).href} target="_blank" rel="noreferrer">Git</a> : <></> } eventName="repo" eventQuery={ params => `owner=${encodeURIComponent(params.owner || "")}` }></GenericList>}/>
            <Route path="/" element={<Navigate to={"0"}/>}/>
            <Route path=":page/:repo/*" element={
              <Routes>
                <Route path=":page" element={<GenericList id={(o: IWorkflowRun) => o.id} hasBack={true} summary={(o: IWorkflowRun) => <span>{o.displayName ?? o.fileName}<br/>RunId: {o.id}, EventName: {o.eventName}<br/>Workflow: {o.fileName}<br/>{o.ref} {o.sha} {o.result ?? "Pending"}</span>} url={(params) => `${ghHostApiUrl}/_apis/v1/Message/workflow/runs?owner=${encodeURIComponent(params.owner || "zero")}&repo=${encodeURIComponent(params.repo || "zero")}&page=${params.page || "0"}`} externalBackUrl={params => gitServerUrl && new URL(`${params.owner}/${params.repo}`, gitServerUrl).href} externalBackLabel={() => "Back to git"} actions={ (run, params) => gitServerUrl ? <a href={new URL(`${params.owner}/${params.repo}/commit/${run.sha}`, gitServerUrl).href} target="_blank" rel="noreferrer">Git</a> : <></> } eventName="workflowrun" eventQuery={ params => `owner=${encodeURIComponent(params.owner || "")}&repo=${encodeURIComponent(params.repo || "")}` }></GenericList>}/>
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
        {/* <Route path=":owner/:repo/:runid/*" element={
        <>
          <Routes>
            <Route path=":page/*" element={<List/>}/>
            <Route path="/" element={<Navigate to={"0"}/>}/>
          </Routes>
          <Routes>
            <Route path=":page/:id/*" element={<JobPage></JobPage>}/>
          </Routes>
        </>}/> */}
      </Routes>
    );
}

export default App;
