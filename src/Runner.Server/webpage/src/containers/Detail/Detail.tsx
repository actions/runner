import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Header } from 'components';
import { IJob, IJobCompletedEvent } from '../../state/store.selectors';
import { Item } from '../../state/example.model';
import styles from './Detail.module.scss';
import Convert from 'ansi-to-html'; 

import Collapsible from 'react-collapsible';
import { ghHostApiUrl } from 'settings';

export interface DetailProps {
    item: Item | null
}

interface ILog {
    id: number,
    location: string
    content: string
}

interface ITimeLine {
    id: string,
    parentId: string,
    Type: string,
    log: ILog | null,
    order: number,
    name: string,
    busy: boolean,
    failed: boolean,
    state: string,
    result: string
}

// interface IJobEvent {
//     repo: string,
//     job: IJob 
// }

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
const artifactUrl = ghHostApiUrl + "/runner/host/_apis/pipelines/workflows/" + runid + "/artifacts"

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


export const DetailContainer : React.FC<DetailProps> = (props) => {
    const [ job, setJob ] = useState<IJob | null>(null);
    const [ timeline, setTimeline ] = useState<ITimeLine[]>([]);
    const [ artifacts, setArtifacts ] = useState<ArtifactResponse[]>([]);
    const [ title, setTitle] = useState<string>("Loading...");
    const { id } = useParams<{id: string}>();
    const { owner, repo } = useParams<{owner: string, repo: string}>();
    const [ errors, setErrors] = useState<string[]>([]);
    var updateTitle = (job: IJob) => {
        setTitle(job.result ? "Job " + job.name + " completed with result: " + job.result : job.name);
    }
    var jobToItem = (job: IJob) : { item: Item | null, job: IJob | null } => { return { item: { id: job.jobId, title: job.name, description: job.timeLineId },job: job}};
    useEffect(() => {
        (async () => {
            try {
                setArtifacts(_ => []);
                if(id === undefined) {
                    setJob(null);
                    setTitle("Please select a Job");
                    setTimeline(e => []);
                    setErrors([]);
                    return;
                }
                var job : IJob | null = await (await (await fetch(ghHostApiUrl + "/_apis/v1/Message?jobid=" + encodeURIComponent(id), { })).json());
                setJob(job);
                updateTitle(job);
                var query = jobToItem(job);
                if(query.job.errors !== null && query.job.errors.length > 0) {
                    setErrors(query.job.errors);
                } else {
                    setErrors([]);
                }
                const item = query.item;
                const timelineId = item ? item.description : null;
                if(timelineId != null) {
                    var timeline = await fetch(ghHostApiUrl + "/_apis/v1/Timeline/" + timelineId, { });
                    if(timeline.status === 200) {
                        var newTimeline = await timeline.json() as ITimeLine[];
                        if(newTimeline != null && newTimeline.length > 0) {
                            newTimeline.sort((a,b) => !a.parentId ? -1 : !b.parentId ? 1 : a.order - b.order);
                            setTimeline(newTimeline);
                        } else {
                            setTimeline([]);
                        }
                    } else {
                        setTitle((query.job.errors !== null && query.job.errors.length > 0) ? "Job " + query.job.name + " failed to run" : (query.job.result ? "Job " + query.job.name + " completed with result: " + query.job.result : "Wait for job " + query.job.name + " to run..."));
                        setTimeline(e => []);
                    }
                }
                if(query.job.runid !== -1) {
                    var artifacts = await listArtifacts(query.job.runid);
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
                }
            } catch(err) {
                setJob(null);
                setTitle("Error: " + err);
                setTimeline(e => []);
                setErrors([]);
            }
        })();
    }, [id, owner, repo])
    useEffect(() => {
        if(job != null) {
            var item = jobToItem(job).item;
            if(item !== null && item.description && item.description !== '' && item.description !== "00000000-0000-0000-0000-000000000000") {
                var source = new EventSource(ghHostApiUrl + "/_apis/v1/TimeLineWebConsoleLog?timelineId="+ item.description);
                try {
                    var missed : ILoglineEvent[] = [];
                    var callback = function(timeline, e:ILoglineEvent) {
                        var s = timeline.find(t => t.id === e.record.stepId);
                        var convert = new Convert({
                            newline: true,
                            escapeXML: true
                        });
                        if(s) {
                            if(s.log == null) {
                                s.log = { id:-1, location: null, content: ""};
                                if(e.record.startLine > 1) {
                                    (async () => {
                                        console.log("Downloading previous log lines of this step...");
                                        var lines = await fetch(ghHostApiUrl + "/_apis/v1/TimeLineWebConsoleLog/" + item.description + "/" + e.record.stepId, { });
                                        if(lines.status === 200) {
                                            var missingLines = await lines.json() as ILogline[];
                                            missingLines.length = e.record.startLine - 1;
                                            s.log.content = missingLines.reduce((prev: string, c : ILogline) => (prev.length > 0 ? prev + "<br/>" : "") + convert.toHtml(c.line), "") + s.log.content;
                                        } else {
                                            console.log("No logs to download..., currently fixes itself");
                                        }
                                    })();
                                }
                            }
                            if (s.log.id === -1) {
                                s.log.content = e.record.value.reduce((prev: string, c : string) => (prev.length > 0 ? prev + "<br/>" : "") + convert.toHtml(c), s.log.content);
                            }
                            return true;
                        }
                        return false;
                    }
                    source.addEventListener ("log", (ev : MessageEvent) => {
                        console.log("new logline " + ev.data);
                        var e = JSON.parse(ev.data) as ILoglineEvent;
                        setTimeline(timeline => {
                            if(callback(timeline, e)) {
                                return [...timeline];
                            }
                            missed.push(e);
                            return timeline;
                        });
                    });
                    source.addEventListener ("timeline", (ev : MessageEvent) => {
                        var e = JSON.parse(ev.data) as ITimeLineEvent;
                        setTitle(e.timeline[0].name);
                        setTimeline(oldtimeline => {
                            var dict = new Map<string, ITimeLine>();
                            for (let i = 0; i < oldtimeline.length; i++) {
                                dict.set(oldtimeline[i].id, oldtimeline[i]);
                            }
                            for (let i = 0; i < e.timeline.length; i++) {
                                if(dict.has(e.timeline[i].id)) {
                                    dict.get(e.timeline[i].id).name = e.timeline[i].name;
                                    dict.get(e.timeline[i].id).result = e.timeline[i].result;
                                    dict.get(e.timeline[i].id).state = e.timeline[i].state;
                                } else {
                                    dict.set(e.timeline[i].id, e.timeline[i]);
                                }
                            }
                            if(e.timeline.length === 0) {
                                // Todo Merge Timelines here
                                return oldtimeline;
                            }
                            
                            var timeline : ITimeLine[] = [];
                            dict.forEach(entry => timeline.push(entry));
                            timeline.sort((a,b) => !a.parentId ? -1 : !b.parentId ? 1 : a.order - b.order);
                            for (; missed.length > 0;) {
                                if(callback(timeline, missed[0])) {
                                    missed.shift();
                                } else {
                                    break;
                                }
                            }
                            return timeline;
                        });
                        // console.log(ev.data)
                    });
                    source.addEventListener("finish", (ev : MessageEvent) => {
                        var e = JSON.parse(ev.data) as IJobCompletedEvent;
                        if(e.jobId === id) {
                            (async function() {
                                job.result = e.result;
                                updateTitle(job);
                            })()
                        }
                    });
                } finally {
                    return () => {
                        source.close();
                    }
                }
            }
        }
        return () => {}
    }, [id, job, owner, repo]);

    return (
        <section className={styles.component}>
        <Header title={title} />
        <main className={styles.main}>
            <div className={styles.text} style={{width: '100%'}}>
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
                            </div>;
                        }
                    }
                })()
                }
                
                {errors.map(e => <div>Error: {e}</div>)}
                {artifacts.map((container: ArtifactResponse) => <div><div>{container.name}</div>{(() => {
                    if(container.files !== undefined) {
                        return container.files.map(file => <div><a href={file.contentLocation}>{file.path}</a></div>);
                    }
                    return <div/>;
                })()}</div>)}
                {(() => {
                    if(timeline.length == 1) {
                        var item = timeline[0];
                        if(!item.busy && !item.failed && (item.log == null || (item.log.id !== -1 && (!item.log.content || item.log.content.length === 0)))) {
                            item.busy = true;
                            (async() => {
                                try {
                                    var convert = new Convert({
                                        newline: true,
                                        escapeXML: true
                                    });
                                    if(item.log == null) {
                                        console.log("Downloading previous log lines of this step...");
                                        const item2 = jobToItem(job).item;
                                        var logs = await fetch(ghHostApiUrl + "/_apis/v1/TimeLineWebConsoleLog/" + item2.description + "/" + item.id, { });
                                        if(logs.status === 200) {
                                            var missingLines = await logs.json() as ILogline[];
                                            item.log = { id: -1, location: null, content: missingLines.reduce((prev: string, c : ILogline) => (prev.length > 0 ? prev + "<br/>" : "") + convert.toHtml(c.line), "") };
                                        } else {
                                            console.log("No logs to download...");
                                        }
                                    } else {
                                        const log = await (await fetch(ghHostApiUrl + "/_apis/v1/Logfiles/" + item.log.id, { })).text();
                                        var lines = log.split('\n');
                                        var offset = '2021-04-02T15:50:14.6619714Z '.length;
                                        var re = /^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}.[0-9]{7}Z /;
                                        lines[0] = convert.toHtml(re.test(lines[0]) ? lines[0].substring(offset) : lines[0]);
                                        item.log.content = lines.reduce((prev, currentValue) => (prev.length > 0 ? prev + "<br/>" : "") + convert.toHtml(re.test(currentValue) ? currentValue.substring(offset) : currentValue));
                                    }
                                } catch {
                                    item.failed = true;
                                } finally {
                                    item.busy = false;
                                    // that.forceUpdate();
                                    setTimeline((t) => {
                                        return [...t];
                                    });
                                }
                            })();
                        }
                        return <div style={{ textAlign: 'left', whiteSpace: 'pre', maxHeight: '100%', overflow: 'auto', fontFamily: "SFMono-Regular,Consolas,Liberation Mono,Menlo,monospace" }} dangerouslySetInnerHTML={{ __html: item.log != null ? item.log.content : "Nothing here" }}></div>
                    }
                    var m = timeline.map((item: ITimeLine, i) =>
                        <Collapsible key={id + item.id} className={styles.Collapsible} open={false} openedClassName={styles.Collapsible} triggerClassName={styles.Collapsible__trigger} triggerOpenedClassName={styles.Collapsible__trigger + " " + styles["is-open"]} contentOuterClassName={styles.Collapsible__contentOuter} contentInnerClassName={styles.Collapsible__contentInner} trigger={(item.result == null ? item.state == null ? "Waiting" : item.state  : item.result) + " - " + item.name} onOpening={() => {
                            if(i != 0 && !item.busy && (item.log == null || (item.log.id !== -1 && (!item.log.content || item.log.content.length === 0)))) {
                                item.busy = true;
                                (async() => {
                                    try {
                                        var convert = new Convert({
                                            newline: true,
                                            escapeXML: true
                                        });
                                        if(item.log == null) {
                                            console.log("Downloading previous log lines of this step...");
                                            const item2 = jobToItem(job).item;
                                            var logs = await fetch(ghHostApiUrl + "/_apis/v1/TimeLineWebConsoleLog/" + item2.description + "/" + item.id, { });
                                            if(logs.status === 200) {
                                                var missingLines = await logs.json() as ILogline[];
                                                item.log = { id: -1, location: null, content: missingLines.reduce((prev: string, c : ILogline) => (prev.length > 0 ? prev + "<br/>" : "") + convert.toHtml(c.line), "") };
                                            } else {
                                                console.log("No logs to download...");
                                            }
                                        } else {
                                            const log = await (await fetch(ghHostApiUrl + "/_apis/v1/Logfiles/" + item.log.id, { })).text();
                                            var lines = log.split('\n');
                                            var offset = '2021-04-02T15:50:14.6619714Z '.length;
                                            var re = /^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}.[0-9]{7}Z /;
                                            lines[0] = convert.toHtml(re.test(lines[0]) ? lines[0].substring(offset) : lines[0]);
                                            item.log.content = lines.reduce((prev, currentValue) => (prev.length > 0 ? prev + "<br/>" : "") + convert.toHtml(re.test(currentValue) ? currentValue.substring(offset) : currentValue));
                                        }
                                    } finally {
                                        item.busy = false;
                                        // that.forceUpdate();
                                        setTimeline((t) => {
                                            return [...t];
                                        });
                                    }
                                })();
                            }
                        }}>
                            <div style={{ textAlign: 'left', whiteSpace: 'pre', maxHeight: '100%', overflow: 'auto', fontFamily: "SFMono-Regular,Consolas,Liberation Mono,Menlo,monospace" }} dangerouslySetInnerHTML={{ __html: item.log != null ? item.log.content : "Nothing here" }}></div>
                        </Collapsible>
                    );
                    m.shift();
                    return m;
                }
                )()}
            </div>
        </main>
        </section>
    );
};