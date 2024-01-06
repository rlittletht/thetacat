using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.Identity.Client;
using TCore.Pipeline;
using Thetacat.Azure;
using Thetacat.Import;
using Thetacat.Logging;
using Thetacat.Model;
using Thetacat.ServiceClient;

namespace Thetacat.Util;

public class MultiPipelineWorker<T> where T : IPipelineBase<T>, new()
{
    private readonly List<ProducerConsumer<T>> m_pipelines = new();
    private readonly int m_pipelineCount;
    private readonly Consumer<T>.ProcessRecordDelegate m_doWork;

    public MultiPipelineWorker(int pipelineCount, Consumer<T>.ProcessRecordDelegate doWork)
    {
        m_pipelineCount = pipelineCount;
        m_doWork = doWork;
    }

    public void Start()
    {
        if (m_pipelines.Count == 0)
        {
            for (int i = 0; i < m_pipelineCount; i++)
            {
                ProducerConsumer<T> newPipeline = new ProducerConsumer<T>(null, m_doWork);
                m_pipelines.Add(newPipeline);
            }
        }

        for (int i = 0; i < m_pipelineCount; i++)
        {
            m_pipelines[i].Start();
        }
    }

    public void Stop()
    {
        for (int i = 0; i < m_pipelineCount; i++)
        {
            m_pipelines[i].Stop();
        }
    }

    private int m_nextPipeline = 0;

    public void QueueWork(T t)
    {
        if (m_pipelines.Count == 0)
            throw new Exception("no pipelines running for work");

        m_pipelines[m_nextPipeline].Producer.QueueRecord(t);
        m_nextPipeline = (m_nextPipeline + 1) % m_pipelineCount;
    }
}