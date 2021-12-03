import React, {useState,useEffect} from 'react';
import { useRouteMatch, Link, useLocation, useParams } from 'react-router-dom';
import { Header, ListItemLink } from 'components';
import { Items } from 'state';
import { ghHostApiUrl } from 'settings';
import Convert from 'ansi-to-html'; 

export interface WorkflowRunAttemptProps {
}
interface IWorkflowRunAttempt {
    attempt: string,
    fileName: string,
    timeLineId: string
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

export const WorkflowRunAttempt: React.FC<WorkflowRunAttemptProps> = (props) => {
    let { url } = useRouteMatch();
    const [ objattempt, setObjattempt ] = useState<IWorkflowRunAttempt>(null);
    const [ timeline, setTimeline ] = useState<ITimeLine[]>([]);
    const { page, owner, repo, run, attempt } = useParams<{page: string, owner: string, repo: string, run: string, attempt: string}>();
    useEffect(() => {
        var apiUrl = `${ghHostApiUrl}/_apis/v1/Message/workflow/run/${run}/attempt/${attempt}?owner=${encodeURIComponent(owner)}&repo=${encodeURIComponent(repo)}&page=${page || "0"}`;
        (async () => {
            var newOwners = JSON.parse((await (await fetch(apiUrl, { })).text())) as IWorkflowRunAttempt;
            setObjattempt(_ => {
                return newOwners;
            });
            var timeline = await fetch(ghHostApiUrl + "/_apis/v1/Timeline/" + newOwners.timeLineId, { });
            if(timeline.status === 200) {
                var newTimeline = await timeline.json() as ITimeLine[];
                if(newTimeline != null && newTimeline.length > 0) {
                    newTimeline.sort((a,b) => !a.parentId ? -1 : !b.parentId ? 1 : a.order - b.order);
                    setTimeline(newTimeline);
                } else {
                    setTimeline([]);
                }
            }
        })();
    }, [page, owner, repo, run, attempt])
    const location = useLocation<string>();
    return (
        <React.Fragment>
            <Header title={`WorkflowRunAttempt ${owner}/${repo}`} hideBackButton={false}/>
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
                                    var logs = await fetch(ghHostApiUrl + "/_apis/v1/TimeLineWebConsoleLog/" + objattempt.timeLineId + "/" + objattempt.timeLineId, { });
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
            })()}
        </React.Fragment>
    );
};

