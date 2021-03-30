import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Header } from 'components';
import { getJobById, IJob } from '../../state/store.selectors';
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
    Type: string,
    log: ILog | null,
    order: Number,
    name: string
}

interface IJobEvent {
    repo: string,
    job: IJob 
}

interface ILogline {
    line : string,
    lineNumber: Number
}

interface IRecord {
    Value: string[],
    StepId: string,
    StartLine: Number
    Count: Number
}

interface ILoglineEvent {
    recordId: string,
    record: IRecord
}

export const DetailContainer : React.FC<DetailProps> = (props) => {
    const [ jobs, setJobs ] = useState<IJob[] | null>([]);
    const [ timeline, setTimeline ] = useState<ITimeLine[]>([]);
    const [ title, setTitle] = useState<string>("Loading...");
    const { id } = useParams();
    const { owner, repo } = useParams();

    useEffect(() => {
        (async () => {
            if(id === undefined) {
                return;
            }
            var njobs : IJob[] | null;
            var _id = Number.parseInt(id);
            if(jobs.length === 0 || jobs.find(x => x.RequestId === _id) == null) {
                njobs = await (await (await fetch(ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/Message", { })).json())
                setJobs(njobs);
            }
            const item = getJobById(njobs || jobs, id).item
            const timelineId = item ? item.description : null;
            if(timelineId != null) {
                var newTimeline = await (await fetch(ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/Timeline/" + timelineId, { })).json() as ITimeLine[];
                if(newTimeline != null && newTimeline.length > 1) {
                    setTitle(newTimeline.shift().name);
                    var convert = new Convert({
                        newline: true
                    });
                    for (const tl of newTimeline) {
                        if (tl.log !== null && tl.log.id !== -1) {
                            const log = await (await fetch(ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/Logfiles/" + tl.log.id, { })).text();
                            tl.log.content = convert.toHtml(log);
                        } else {
                            try {
                                tl.log = { id:-1, location: null, content: (await (await fetch(ghHostApiUrl + "/" + owner + "/" + repo + "/_apis/v1/TimeLineWebConsoleLog/" + timelineId + "/" + tl.id, { })).json() as ILogline[]).reduce((prev: string, c : ILogline) => prev + "<br/>" + convert.toHtml(c.line), "")};
                            } catch {

                            }
                        }
                    }
                }
                setTimeline(newTimeline);
            }
        })();
    }, [id, jobs, owner, repo])
    useEffect(() => {
        if(id !== undefined && id !== null && id.length > 0) {
            var item = getJobById(jobs, id).item;
            if(item !== null) {
                var source = new EventSource( + "/" + owner + "/" + repo + "/_apis/v1/TimeLineWebConsoleLog?timelineId="+ item.description);
                source.addEventListener ("log", (ev : MessageEvent) => {
                    console.log("new logline " + ev.data);
                    var e = JSON.parse(ev.data) as ILoglineEvent;
                    setTimeline(timeline => {
                        var s = timeline.find(t => t.id === e.record.StepId);
                        var convert = new Convert({
                            newline: true,
                            escapeXML: true
                        });
                        if(s != null) {
                            if(s.log == null) {
                                s.log = { id:-1, location: null, content: ""};
                            }
                            if (s.log.id === -1) {
                                s.log.content = e.record.Value.reduce((prev: string, c : string) => prev + "<br/>" + convert.toHtml(c), s.log.content);
                            }
                        } else {
                            (async () => {
                                
                            })();
                        }
                        return [...timeline];
                    });
                    
                });
                return () => {
                    source.close();
                }
            }
        }
        return () => {}
    }, [id, jobs, owner, repo]);

    return (
        <section className={styles.component}>
        <Header title={title} />
        <main className={styles.main}>
            <div className={styles.text} style={{width: '100%'}}> 
                {timeline.map((item: ITimeLine) =>
                    <Collapsible key={id + item.id} className={styles.Collapsible} openedClassName={styles.Collapsible} triggerClassName={styles.Collapsible__trigger} triggerOpenedClassName={styles.Collapsible__trigger + " " + styles["is-open"]} contentOuterClassName={styles.Collapsible__contentOuter} contentInnerClassName={styles.Collapsible__contentInner} trigger={item.name}>
                        <div style={{ textAlign: 'left', whiteSpace: 'nowrap', maxHeight: '100%', overflow: 'auto' }} dangerouslySetInnerHTML={{ __html: item.log != null ? item.log.content : "Nothing here" }}></div>
                    </Collapsible>
                )}
            </div>
        </main>
        </section>
    );
};